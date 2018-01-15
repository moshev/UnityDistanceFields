using System;
using System.Collections.Generic;
using System.Threading;

/// <summary>
/// Report progress on operation
/// </summary>
public class ProgressReport
{
    public const int STATE_NOT_STARTED = 0;
    public const int STATE_RUNNING = 1;
    public const int STATE_FINISHED = 2;
    public const int STATE_CANCELLED = 3;

    public struct State
    {
        public int runStatus;
        public double progress;
        public string message;

        public State(string _message, double _progress)
        {
            runStatus = STATE_NOT_STARTED;
            message = _message;
            progress = _progress;
        }
    }

    private State state;

    /// <summary>
    /// Executed in main thread once progress ends
    /// </summary>
    public delegate void MainThreadCallback();

    private MainThreadCallback callback;

    /// <summary>
    ///  Tasks executed once per tick in the GUI
    /// </summary>
    private Stack<MainThreadCallback> queuedTasks = new Stack<MainThreadCallback>();

    private int wasChanged = 0;

    public MainThreadCallback Callback
    {
        set
        {
            callback = value;
        }
    }

    public ProgressReport()
    {
        state.runStatus = STATE_NOT_STARTED;
        state.progress = 0;
        state.message = "";
    }

    /// <summary>
    /// Whether this progress has been changed since last time. Also clears the status.
    /// </summary>
    public bool Changed
    {
        get
        {
            int changed = Interlocked.Exchange(ref wasChanged, 0);
            return changed != 0;
        }
    }

    public State CurrentState
    {
        get
        {
            State result;
            result.runStatus = Interlocked.CompareExchange(ref state.runStatus, STATE_NOT_STARTED, STATE_NOT_STARTED);
            result.progress = Interlocked.CompareExchange(ref state.progress, 0.0, 0.0);
            result.message = Interlocked.CompareExchange(ref state.message, null, null);
            return result;
        }

        set
        {
            state = value;
        }
    }

    public void StartProgress(string message)
    {
        Interlocked.Exchange(ref state.runStatus, STATE_RUNNING);
        Interlocked.Exchange(ref state.progress, 0.0);
        Interlocked.Exchange(ref state.message, message);
        Thread.MemoryBarrier();
        Interlocked.Increment(ref wasChanged);
    }

    public void SetProgress(double progress)
    {
        Interlocked.Exchange(ref state.progress, progress);
        Thread.MemoryBarrier();
        Interlocked.Increment(ref wasChanged);
    }

    public void EndProgress()
    {
        Interlocked.Exchange(ref state.runStatus, STATE_FINISHED);
        Thread.MemoryBarrier();
        Interlocked.Increment(ref wasChanged);
    }

    public void CancelProgress()
    {
        int oldState = Interlocked.CompareExchange(ref state.runStatus, STATE_CANCELLED, STATE_RUNNING);
        if (oldState == STATE_RUNNING)
        {
            Interlocked.Exchange(ref callback, null);
        }
        Thread.MemoryBarrier();
        Interlocked.Increment(ref wasChanged);
    }

    public void RunMainThreadCallback()
    {
        if (callback != null)
        {
            callback();
            callback = null;
        }
    }

    public void RunQueuedTasks()
    {
        Monitor.Enter(queuedTasks);
        try
        {
            while (true)
            {
                MainThreadCallback task;
                try
                {
                    task = queuedTasks.Pop();
                }
                catch (InvalidOperationException)
                {
                    break;
                }
                task();
            }
        }
        finally
        {
            Interlocked.Increment(ref wasChanged);
            Monitor.Exit(queuedTasks);
        }
    }

    public void EnqueueTask(MainThreadCallback task)
    {
        if (task == null) return;
        Monitor.Enter(queuedTasks);
        try
        {
            queuedTasks.Push(task);
        }
        finally
        {
            Interlocked.Increment(ref wasChanged);
            Monitor.Exit(queuedTasks);
        }
    }
}

/// <summary>
/// Blocking multiple producer - multiple consumer bounded queue
/// </summary>
/// <typeparam name="T"></typeparam>
public class BlockingQueue<T> where T : class
{
    private T[] items;
    private volatile int getIdx;
    private volatile int setIdx;

    /// monitor used to wait for enqueue operations
    private object enqueueMonitor;

    /// monitor used to wait for dequeue operations
    private object dequeueMonitor;

    private object overallMonitor;

    public BlockingQueue(int capacity)
    {
        items = new T[capacity + 1];
        getIdx = 0;
        setIdx = 0;
        enqueueMonitor = new object();
        dequeueMonitor = new object();
        overallMonitor = new object();
    }

    public void Enqueue(T item)
    {
        Monitor.Enter(overallMonitor);
        for (;;)
        {
            int myGetIdx = getIdx;
            int mySetIdx = setIdx;
            Thread.MemoryBarrier();
            int nextSetIdx = (mySetIdx + 1) % items.Length;
            if (nextSetIdx == myGetIdx)
            {
                Monitor.Enter(dequeueMonitor);
                Monitor.Exit(overallMonitor);
                Monitor.Wait(dequeueMonitor);
                Monitor.Enter(overallMonitor);
                Monitor.Exit(dequeueMonitor);
                continue;
            }
            items[mySetIdx] = item;
            setIdx = nextSetIdx;
            Thread.MemoryBarrier();
            Monitor.Enter(enqueueMonitor);
            Monitor.PulseAll(enqueueMonitor);
            Monitor.Exit(enqueueMonitor);
            break;
        }
        Monitor.Exit(overallMonitor);
    }

    public bool TryDequeue(ref T item)
    {
        bool result;
        Monitor.Enter(overallMonitor);
        int myGetIdx = getIdx;
        int mySetIdx = setIdx;
        Thread.MemoryBarrier();
        if (myGetIdx == mySetIdx)
        {
            result = false;
        }
        else
        {
            item = items[myGetIdx];
            getIdx = (myGetIdx + 1) % items.Length;
            Thread.MemoryBarrier();
            Monitor.Enter(dequeueMonitor);
            Monitor.PulseAll(dequeueMonitor);
            Monitor.Exit(dequeueMonitor);
            result = true;
        }
        Monitor.Exit(overallMonitor);
        return result;
    }

    public T Dequeue()
    {
        T result;
        Monitor.Enter(overallMonitor);
        for (;;)
        {
            int myGetIdx = getIdx;
            int mySetIdx = setIdx;
            Thread.MemoryBarrier();
            if (myGetIdx == mySetIdx)
            {
                Monitor.Enter(enqueueMonitor);
                Monitor.Exit(overallMonitor);
                Monitor.Wait(enqueueMonitor);
                Monitor.Enter(overallMonitor);
                Monitor.Exit(enqueueMonitor);
                continue;
            }
            getIdx = (myGetIdx + 1) % items.Length;
            result = items[myGetIdx];
            Thread.MemoryBarrier();
            Monitor.Enter(dequeueMonitor);
            Monitor.PulseAll(dequeueMonitor);
            Monitor.Exit(dequeueMonitor);
            break;
        }
        Monitor.Exit(overallMonitor);
        return result;
    }
}