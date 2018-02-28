using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEditor;
using System.Threading;

internal struct RayContext
{
    public Vector3 p;
    public Vector3 dir;

    public void WriteTo(float[] array, int i)
    {
        array[6 * i + 0] = p.x;
        array[6 * i + 1] = p.y;
        array[6 * i + 2] = p.z;
        array[6 * i + 3] = dir.x;
        array[6 * i + 4] = dir.y;
        array[6 * i + 5] = dir.z;
    }

    public void ReadFrom(float[] array, int i)
    {
        p.x = array[6 * i + 0];
        p.y = array[6 * i + 1];
        p.z = array[6 * i + 2];
        dir.x = array[6 * i + 3];
        dir.y = array[6 * i + 4];
        dir.z = array[6 * i + 5];
    }

    public const int floatSize = 6;
}

internal struct RayResult
{
    public Vector3 p;
    public Vector3 n;
    public float distance;

    public void WriteTo(float[] array, int i)
    {
        array[7 * i + 0] = p.x;
        array[7 * i + 1] = p.y;
        array[7 * i + 2] = p.z;
        array[7 * i + 3] = n.x;
        array[7 * i + 4] = n.y;
        array[7 * i + 5] = n.z;
        array[7 * i + 6] = distance;
    }

    public void ReadFrom(float[] array, int i)
    {
        p.x = array[7 * i + 0];
        p.y = array[7 * i + 1];
        p.z = array[7 * i + 2];
        n.x = array[7 * i + 3];
        n.y = array[7 * i + 4];
        n.z = array[7 * i + 5];
        distance = array[7 * i + 6];
    }

    public String DebugDump()
    {
        return String.Format("{0},{1},{2},{3},{4},{5},{6}",
            p.x,
            p.y,
            p.z,
            n.x,
            n.y,
            n.z,
            distance
            );
    }

    public const int floatSize = 7;
}

internal struct GridCube
{
    public int startEdge;
    public int endEdge;
    public Vector3 v;
}

public class DFNodeMesher
{
    public ComputeShader distanceEstimator;
    public DFNode rootNode;
    public Material material; // Material containing shader properties
    public float gridRadius = 8.0f;

    public int gridSize = 48;

    public bool showErrorEdges = false;

    public bool debugAlwaysShowEdges = false;

    private float cornerScale;

    // Number of threads in the compute shader
    private const int computeThreads = 128;

    // How many batches of threads to run at once (at most)
    private const int shaderBatches = 32;

    private int kernelDistanceMain;
    private int kernelRaymarchMain;

    private ComputeBuffer shaderInputBuffer;
    private ComputeBuffer shaderOutputBuffer;

    private AutoResetEvent computeEvent = new AutoResetEvent(false);

    public enum AlgorithmStep
    {
        NotStarted,
        DistanceCalculated,
        EdgeIntersectionsFound,
        VerticesConstructed,
        Finished,
    }

    private AlgorithmStep currentStep = AlgorithmStep.NotStarted;
    private ProgressReport currentProgress = null;

    public AlgorithmStep CurrentStep
    {
        get
        {
            return currentStep;
        }
    }

    private float[,,] distances;
    private List<GridEdge> edgesCrossingSurface;
    private List<GridCube> netCubes;
    private List<int> netCubesEdges; // Variable number of edges per cube
    private List<int> netEdgesCubes; // 4 cubes per edge
    private Dictionary<GridCoordinate, int> netCubesMap;

    public static Vector3[] corners = new Vector3[]
    {
        new Vector3(-0.5f, -0.5f, +0.5f),
        new Vector3(+0.5f, -0.5f, +0.5f),
        new Vector3(+0.5f, +0.5f, +0.5f),
        new Vector3(-0.5f, +0.5f, +0.5f),
        new Vector3(+0.5f, -0.5f, -0.5f),
        new Vector3(-0.5f, -0.5f, -0.5f),
        new Vector3(-0.5f, +0.5f, -0.5f),
        new Vector3(+0.5f, +0.5f, -0.5f),
    };

    public static int[] quadCorners = new int[]
    {
        1,4,7,2,
        5,0,3,6,
        3,2,7,6,
        0,5,4,1,
        0,1,2,3,
        4,5,6,7,
    };

    public DFNodeMesher()
    {
    }

    private void InitBuffers()
    {
        int bufsz = computeThreads * shaderBatches;
        shaderInputBuffer = new ComputeBuffer(bufsz, RayContext.floatSize * sizeof(float));
        shaderOutputBuffer = new ComputeBuffer(bufsz, RayResult.floatSize * sizeof(float));
    }

    private void DisposeBuffers()
    {
        shaderInputBuffer.Dispose();
        shaderOutputBuffer.Dispose();
        shaderInputBuffer = null;
        shaderOutputBuffer = null;
    }

    public void InitKernel()
    {
        kernelDistanceMain = distanceEstimator.FindKernel("DistanceMain");
        kernelRaymarchMain = distanceEstimator.FindKernel("RaymarchMain");
        if (kernelDistanceMain < 0 || kernelRaymarchMain < 0)
        {
            throw new Exception("Can't find kernel DistanceMain or RaymarchMain");
        }
        rootNode.SetTransformsInComputeShader(distanceEstimator, false);
        int nProperties = ShaderUtil.GetPropertyCount(material.shader);
        for (int i = 0; i < nProperties; i++)
        {
            string name = ShaderUtil.GetPropertyName(material.shader, i);
            if (name.StartsWith("_transform_")) continue;
            ShaderUtil.ShaderPropertyType type = ShaderUtil.GetPropertyType(material.shader, i);
            if (type == ShaderUtil.ShaderPropertyType.Float)
            {
                distanceEstimator.SetFloat(name, material.GetFloat(name));
            }
        }
    }

    public void AlgorithmClear()
    {
        InitKernel();
        currentStep = AlgorithmStep.NotStarted;
        cornerScale = 2.0f * gridRadius / gridSize;
    }

    public delegate void TaskSingle();

    public delegate void TaskMulti(int threadId, int numThreads);

    private void StartTask(ProgressReport progressReport, string message, TaskSingle task)
    {
        currentProgress = progressReport;
        currentProgress.StartProgress(message);
        Thread worker = new Thread(delegate ()
        {
            try
            {
                task();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            progressReport.EndProgress();
        });
        worker.IsBackground = true;
        worker.Start();
    }

    private void StartTask(ProgressReport progressReport, string message, TaskMulti task)
    {
        currentProgress = progressReport;
        currentProgress.StartProgress(message);
        int numThreads = Environment.ProcessorCount;
        if (numThreads > 1) numThreads--; // leave one thread free
        Thread[] workers = new Thread[numThreads];
        int completed = 0;
        for (int i = 0; i < numThreads; i++)
        {
            workers[i] = new Thread(delegate ()
            {
                try
                {
                    task(i, numThreads);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
                if (Interlocked.Increment(ref completed) == numThreads)
                {
                    progressReport.EndProgress();
                }
            });
            workers[i].IsBackground = true;
            workers[i].Start();
        }
    }

    public void AlgorithmCalculateDistances(ProgressReport progressReport)
    {
        //currentProgress = progressReport;
        //currentProgress.StartProgress("Calculating distances");
        //TaskCalculateDistances();
        //progressReport.EndProgress();
        InitKernel();
        InitBuffers();
        progressReport.Callback = DisposeBuffers;
        StartTask(progressReport, "Calculating distances", TaskCalculateDistances);
    }

    private void InvokeShader(float[] vinput, float[] voutput, int nInputs, int kernelIndex, int maxIters = 256)
    {
        currentProgress.EnqueueTask(delegate ()
        {
            float[] _vinput = vinput;
            float[] _voutput = voutput;
            int _nInputs = nInputs;
            int _kernelIndex = kernelIndex;
            int _maxIters = maxIters;
            Debug.Assert(distanceEstimator != null);
            for (int i = nInputs * RayContext.floatSize; i < vinput.Length; i++)
            {
                vinput[i] = 0;
            }
            shaderInputBuffer.SetData(vinput);
            int tgroups = (nInputs + computeThreads - 1) / computeThreads;
            //Debug.Log("Dispatching " + tgroups + " thread groups");
            distanceEstimator.SetInt("maxIters", maxIters);
            distanceEstimator.SetBuffer(kernelIndex, "_input", shaderInputBuffer);
            distanceEstimator.SetBuffer(kernelIndex, "_output", shaderOutputBuffer);
            distanceEstimator.Dispatch(kernelIndex, tgroups, 1, 1);
            shaderOutputBuffer.GetData(voutput);
            computeEvent.Set();
        });
        computeEvent.WaitOne();
    }

    // Fill distances from float that is RayResult output from compute shader in the inclusive range (i1,j1,k1) to (i2,j2,k2)
    private void FillDistances(float[] output, int i1, int j1, int k1, int i2, int j2, int k2)
    {
        int bufIdx = 0;
        // inner loop limits change from execution to execution;
        int _j1 = j1, _j2 = gridSize - 1, _i1 = i1, _i2 = gridSize - 1;
        for (int k = k1; k <= k2; k++)
        {
            if (k == k2) _j2 = j2;
            for (int j = _j1; j <= _j2; j++)
            {
                if (k == k2 && j == j2) _i2 = i2;
                for (int i = _i1; i <= _i2; i++)
                {
                    RayResult res = new RayResult();
                    //Debug.Log(String.Format("Read from {0} {1} {2} idx {3}", i, j, k, bufIdx));
                    res.ReadFrom(output, bufIdx++);
                    distances[i, j, k] = res.distance;
                    if ((i + j * gridSize + k * gridSize * gridSize) % 1021 == 0)
                    {
                        //Debug.Log(String.Format("Distance {0} {1} {2} = {3}", i, j, k, res.distance));
                    }
                }
                _i1 = 0;
            }
            _j1 = 0;
        }
    }

    private void TaskCalculateDistances()
    {
        StreamWriter logw = new StreamWriter("C:\\Users\\moshev\\Documents\\distlog.txt");
        Debug.Assert(currentProgress != null);
        distances = new float[gridSize, gridSize, gridSize];
        // buffer size in number of RayContext instances
        int bufsz = computeThreads * shaderBatches;
        float[] input = new float[bufsz * RayContext.floatSize];
        float[] output = new float[bufsz * RayResult.floatSize];
        int bufferIdx = 0;
        int iStart = 0, jStart = 0, kStart = 0;
        for (int k = 0; k < gridSize; k++)
        {
            for (int j = 0; j < gridSize; j++)
            {
                for (int i = 0; i < gridSize; i++)
                {
                    if (iStart < 0) iStart = i;
                    if (jStart < 0) jStart = j;
                    if (kStart < 0) kStart = k;
                    RayContext ctx;
                    ctx.p = VectorFromIndices(i, j, k);
                    if ((i + j * gridSize + k * gridSize * gridSize) % 1021 == 0)
                    {
                        //Debug.Log(String.Format("World position for {0} {1} {2} is {3} {4} {5}", i, j, k, ctx.p.x, ctx.p.y, ctx.p.z));
                    }
                    ctx.dir = Vector3.zero;
                    ctx.WriteTo(input, bufferIdx++);
                    if (bufferIdx == bufsz)
                    {
                        logw.Write(String.Format("Filling from {0} {1} {2} to {3} {4} {5} - {6} elements\n", iStart, jStart, kStart, i, j, k, bufsz));
                        logw.Flush();
                        InvokeShader(input, output, bufsz, kernelDistanceMain);
                        FillDistances(output, iStart, jStart, kStart, i, j, k);
                        iStart = -1;
                        jStart = -1;
                        kStart = -1;
                        bufferIdx = 0;
                    }
                }
            }
            currentProgress.SetProgress((k + 1) / (double)gridSize);
        }
        if (bufferIdx > 0)
        {
            InvokeShader(input, output, bufferIdx, kernelDistanceMain);
            FillDistances(output, iStart, jStart, kStart, gridSize - 1, gridSize - 1, gridSize - 1);
        }
        currentStep = AlgorithmStep.DistanceCalculated;
    }

    public void AlgorithmFindEdgeIntersections(ProgressReport progressReport)
    {
        InitKernel();
        InitBuffers();
        progressReport.Callback = DisposeBuffers;
        StartTask(progressReport, "Finding edge intersections", TaskFindEdgeIntersections);
    }

    public void AdjustAndInsertEdges(float[] shaderOutput, GridEdge[] pendingEdges, List<GridEdge> edgesList, int nEdges)
    {
        for (int i = 0; i < nEdges; i++)
        {
            GridEdge e = pendingEdges[i];
            RayResult res = new RayResult();
            res.ReadFrom(shaderOutput, i);
            Vector3 v0 = VectorFromCoordinate(e.c0);
            Vector3 v1 = VectorFromCoordinate(e.c1);
            float t = Vector3.Dot(v0 - v1, res.p - v1) / Vector3.Dot(v0 - v1, v0 - v1);
            /*
            if (i % 37 == 0)
            {
                Vector3 pold = e.t * v0 + (1 - e.t) * v1;
                Debug.Log(String.Format("Edge {0}->{1} ({4}->{5}) adjusting t from {2} to {3}, e.p={6}, e.distance={7};{8} res={9}", e.c0, e.c1, e.t, t, v0, v1, pold,
                    distances[e.c0.i, e.c0.j, e.c0.k], distances[e.c1.i, e.c1.j, e.c1.k], res.DebugDump()));
            }
            */
            e.t = t;
            e.normal = res.n;
            edgesList.Add(e);
        }
    }

    public void TaskFindEdgeIntersections()
    {
        Debug.Assert(currentProgress != null);
        // buffer size in number of RayContext instances
        int bufsz = computeThreads * shaderBatches;
        // shader input
        float[] input = new float[bufsz * RayContext.floatSize];
        // shader output
        float[] output = new float[bufsz * RayResult.floatSize];
        // shader output converted to edges
        GridEdge[] pendingEdges = new GridEdge[bufsz];
        int bufferIdx = 0;
        edgesCrossingSurface = new List<GridEdge>();
        for (int k = 0; k < gridSize; k++)
        {
            for (int j = 0; j < gridSize; j++)
            {
                for (int i = 0; i < gridSize; i++)
                {
                    // only consider edges beginning with vertices that lie inside the volume
                    if (distances[i, j, k] > 0) continue;
                    GridCoordinate[] neighbours = new GridCoordinate[]
                    {
                        new GridCoordinate(i, j, k-1),
                        new GridCoordinate(i, j-1, k),
                        new GridCoordinate(i-1, j, k),
                        new GridCoordinate(i+1, j, k),
                        new GridCoordinate(i, j+1, k),
                        new GridCoordinate(i, j, k+1),
                    };
                    foreach (GridCoordinate neighbour in neighbours)
                    {
                        if (neighbour.i < 0 || neighbour.i >= gridSize) continue;
                        if (neighbour.j < 0 || neighbour.j >= gridSize) continue;
                        if (neighbour.k < 0 || neighbour.k >= gridSize) continue;
                        if (distances[neighbour.i, neighbour.j, neighbour.k] <= 0) continue;
                        // distances is positive, add edge
                        GridEdge e = new GridEdge(
                                new GridCoordinate(i, j, k),
                                new GridCoordinate(neighbour.i, neighbour.j, neighbour.k),
                                distances[i, j, k],
                                distances[neighbour.i, neighbour.j, neighbour.k]);
                        Vector3 v0 = VectorFromCoordinate(e.c0);
                        Vector3 v1 = VectorFromCoordinate(e.c1);
                        float t = e.t;
                        RayContext ctx;
                        ctx.p = t * v0 + (1 - t) * v1;
                        ctx.dir = Vector3.Normalize(v0 - v1);
                        ctx.WriteTo(input, bufferIdx);
                        pendingEdges[bufferIdx] = e;
                        bufferIdx++;
                        if (bufferIdx == bufsz)
                        {
                            InvokeShader(input, output, bufferIdx, kernelRaymarchMain, 8);
                            AdjustAndInsertEdges(output, pendingEdges, edgesCrossingSurface, bufferIdx);
                            bufferIdx = 0;
                        }
                    }
                }
            }
            currentProgress.SetProgress((k + 1) / (double)gridSize);
        }
        if (bufferIdx > 0)
        {
            InvokeShader(input, output, bufferIdx, kernelRaymarchMain, 8);
            AdjustAndInsertEdges(output, pendingEdges, edgesCrossingSurface, bufferIdx);
        }
        currentStep = AlgorithmStep.EdgeIntersectionsFound;
    }

    public void AlgorithmConstructVertices(ProgressReport progressReport)
    {
        InitKernel();
        StartTask(progressReport, "Constructing vertices", TaskConstructVertices);
    }

    public void TaskConstructVertices()
    {
        netCubes = new List<GridCube>();
        netCubesMap = new Dictionary<GridCoordinate, int>();
        netEdgesCubes = new List<int>(4 * edgesCrossingSurface.Count);
        for (int edgeIdx = 0; edgeIdx < edgesCrossingSurface.Count; edgeIdx++)
        {
            GridEdge edge = edgesCrossingSurface[edgeIdx];
            GridCoordinate c0 = edge.c0;
            GridCoordinate c1 = edge.c1;
            Debug.Assert(c0.CompareTo(c1) != 0);
            GridCoordinate cBase;
            if (c0.CompareTo(c1) < 0)
            {
                cBase = c0;
            }
            else
            {
                cBase = c1;
            }
            int di = 1 - Math.Abs(c0.i - c1.i);
            int dj = 1 - Math.Abs(c0.j - c1.j);
            int dk = 1 - Math.Abs(c0.k - c1.k);
            int iidx = 0;
            int[] indices = new int[4];
            for (int k = cBase.k - dk; k <= cBase.k; k++)
            {
                if (k < 0 || k >= gridSize) continue;
                for (int j = cBase.j - dj; j <= cBase.j; j++)
                {
                    if (j < 0 || j >= gridSize) continue;
                    for (int i = cBase.i - di; i <= cBase.i; i++)
                    {
                        if (i < 0 || i >= gridSize) continue;
                        GridCoordinate cCube = new GridCoordinate(i, j, k);
                        int cIdx;
                        if (netCubesMap.ContainsKey(cCube))
                        {
                            cIdx = netCubesMap[cCube];
                            GridCube cube = netCubes[cIdx];
                            cube.endEdge += 1;
                            netCubes[cIdx] = cube;
                        }
                        else
                        {
                            GridCube cube;
                            cube.startEdge = 0;
                            cube.endEdge = 1;
                            cube.v = Vector3.zero;
                            cIdx = netCubes.Count;
                            netCubes.Add(cube);
                            netCubesMap[cCube] = cIdx;
                        }
                        indices[iidx++] = cIdx;
                    }
                }
            }
            for (int i = 0; i < 4; i++)
            {
                netEdgesCubes.Add(indices[i]);
            }
            currentProgress.SetProgress((1.0 / 3.0) * edgeIdx / (double)(edgesCrossingSurface.Count));
        }
        int totalEdges = 0;
        for (int i = 0; i < netCubes.Count; i++)
        {
            GridCube cube = netCubes[i];
            int end = cube.endEdge;
            cube.startEdge += totalEdges;
            cube.endEdge += totalEdges;
            totalEdges += end;
            netCubes[i] = cube;
        }
        netCubesEdges = new List<int>(totalEdges);
        for (int i = 0; i < totalEdges; i++) netCubesEdges.Add(-1);
        for (int edgeIdx = 0; edgeIdx < edgesCrossingSurface.Count(); edgeIdx++)
        {
            GridEdge edge = edgesCrossingSurface[edgeIdx];
            GridCoordinate c0 = edge.c0;
            GridCoordinate c1 = edge.c1;
            Debug.Assert(c0.CompareTo(c1) != 0);
            GridCoordinate cBase;
            if (c0.CompareTo(c1) < 0)
            {
                cBase = c0;
            }
            else
            {
                cBase = c1;
            }
            int di = 1 - Math.Abs(c0.i - c1.i);
            int dj = 1 - Math.Abs(c0.j - c1.j);
            int dk = 1 - Math.Abs(c0.k - c1.k);
            for (int k = cBase.k - dk; k <= cBase.k; k++)
            {
                if (k < 0 || k >= gridSize) continue;
                for (int j = cBase.j - dj; j <= cBase.j; j++)
                {
                    if (j < 0 || j >= gridSize) continue;
                    for (int i = cBase.i - di; i <= cBase.i; i++)
                    {
                        if (i < 0 || i >= gridSize) continue;
                        GridCoordinate cCube = new GridCoordinate(i, j, k);
                        int cIdx;
                        if (netCubesMap.ContainsKey(cCube))
                        {
                            cIdx = netCubesMap[cCube];
                            GridCube cube = netCubes[cIdx];
                            for (int eIdx = cube.startEdge; eIdx < cube.endEdge; eIdx++)
                            {
                                if (netCubesEdges[eIdx] < 0)
                                {
                                    netCubesEdges[eIdx] = edgeIdx;
                                }
                            }
                        }
                        else
                        {
                            Debug.LogError(String.Format("Error on cube {}", cCube));
                        }
                    }
                }
            }
            currentProgress.SetProgress((1.0 / 3.0) + (1.0 / 3.0) * edgeIdx / (double)(edgesCrossingSurface.Count));
        }
        for (int cubeIdx = 0; cubeIdx < netCubes.Count; cubeIdx++)
        {
            GridCube cube = netCubes[cubeIdx];
            Vector3 sum = Vector3.zero;
            for (int edgeIdx = cube.startEdge; edgeIdx < cube.endEdge; edgeIdx++)
            {
                GridEdge edge = edgesCrossingSurface[netCubesEdges[edgeIdx]];
                Vector3 v0 = VectorFromCoordinate(edge.c0);
                Vector3 v1 = VectorFromCoordinate(edge.c1);
                float t = edge.t;
                Vector3 v = t * v0 + (1 - t) * v1;
                sum += v;
            }
            cube.v = sum / (cube.endEdge - cube.startEdge);
            netCubes[cubeIdx] = cube;
            currentProgress.SetProgress((2.0 / 3.0) + (1.0 / 3.0) * cubeIdx / (double)netCubes.Count);
        }
        /*
        int errors = 0;
        for (int k = 0; k < gridSize - 1; k++)
        {
            for (int j = 0; j < gridSize - 1; j++)
            {
                for (int i = 0; i < gridSize - 1; i++)
                {
                    int nEdges = 0;
                    // Sum of the intersection points
                    Vector3 sum = Vector3.zero;
                    // coordinate of bottom front left vertex
                    GridCoordinate cBase = new GridCoordinate(i, j, k);
                    for (int edge = 0; edge < 12; edge++)
                    {
                        int iV0 = voxelEdgeVertex0[edge];
                        int iV1 = voxelEdgeVertex1[edge];
                        GridCoordinate c0 = UnpackCubeVertex(iV0) + cBase;
                        GridCoordinate c1 = UnpackCubeVertex(iV1) + cBase;
                        float d0 = distances[c0.i, c0.j, c0.k];
                        float d1 = distances[c1.i, c1.j, c1.k];
                        if (d0 > d1)
                        {
                            float tmpd = d0;
                            d0 = d1;
                            d1 = tmpd;
                            GridCoordinate tmpc = c0;
                            c0 = c1;
                            c1 = tmpc;
                        }
                        if (d0 > 0 || d1 <= 0) continue;
                        GridEdge gridEdge = new GridEdge();
                        bool found = edgesCrossingSurface.Get(ref gridEdge, c0,
                            (GridEdge e) => e.c1 == c1);
                        if (!found)
                        {
                            //Debug.Log(String.Format(
                            //    "Edge not found {0}->{1} of voxel {2}",
                            //    c0, c1, cBase));
                            errors++;
                            gridEdge.c0 = c0;
                            gridEdge.c1 = c1;
                            errorEdges.Add(gridEdge);
                            continue;
                        }
                        Vector3 v0 = VectorFromCoordinate(gridEdge.c0);
                        Vector3 v1 = VectorFromCoordinate(gridEdge.c1);
                        float t = gridEdge.t;
                        Vector3 v = t * v0 + (1 - t) * v1;
                        sum += v;
                        nEdges++;
                    }
                    if (nEdges > 0)
                    {
                        sum /= nEdges;
                        Monitor.Enter(netVertices);
                        netVertices.Add(IndexedVector3.Create(sum), cBase);
                        Monitor.Exit(netVertices);
                    }
                }
            }
            currentProgress.SetProgress((k + 1) / (double)(gridSize - 1));
        }
        Debug.Log("Total error edges " + errors);
        */
        currentStep = AlgorithmStep.VerticesConstructed;
    }

    private class CreateMeshResult
    {
        public volatile Vector3[] vertices;
        public volatile Vector3[] normals;
        public volatile int[] triangles;
    }

    public void AlgorithmCreateMesh(ProgressReport progressReport, MeshFilter mf)
    {
        if (netCubes.Count > 65000)
        {
            Debug.Log(String.Format("Refusing to create mesh with more than 65k vertices: {0}", netCubes.Count));
            return;
        }
        if (mf == null)
        {
            Debug.Log("No mesh filter assigned!");
            return;
        }
        InitKernel();
        InitBuffers();
        mf.mesh = null;
        CreateMeshResult result = new CreateMeshResult();
        progressReport.Callback = delegate ()
        {
            DisposeBuffers();
            Debug.Log(String.Format("Assigning mesh with {0} vertices and {1} triangles",
                result.vertices.Length, result.triangles.Length / 3));
            Mesh mesh = new Mesh();
            mesh.vertices = result.vertices;
            mesh.normals = result.normals;
            mesh.triangles = result.triangles;
            mf.mesh = mesh;
        };
        StartTask(progressReport, "Creating mesh", () => TaskCreateMesh(result));
    }

    public void AlgorithmWriteMesh(ProgressReport progressReport, string path)
    {
        InitKernel();
        InitBuffers();
        CreateMeshResult result = new CreateMeshResult();
        progressReport.Callback = delegate ()
        {
            DisposeBuffers();
            StreamWriter w = new StreamWriter(path, false, Encoding.ASCII);
            Debug.Log(String.Format("Writing mesh with {0} vertices and {1} triangles",
                result.vertices.Length, result.triangles.Length / 3));
            w.Write("o Object\n");
            foreach (Vector3 v in result.vertices)
            {
                w.Write(String.Format("v {0} {1} {2}\n", v.x, v.y, v.z));
            }
            foreach (Vector3 n in result.normals)
            {
                w.Write(String.Format("vn {0} {1} {2}\n", n.x, n.y, n.z));
            }
            for (int i = 0; i < result.triangles.Length; i += 3)
            {
                w.Write(String.Format("f {0}//{0} {1}//{1} {2}//{2}\n",
                    result.triangles[i + 0] + 1,
                    result.triangles[i + 1] + 1,
                    result.triangles[i + 2] + 1));
            }
            w.Close();
        };
        StartTask(progressReport, "Creating mesh", () => TaskCreateMesh(result));
    }

    private void TaskCreateMesh(CreateMeshResult outResult)
    {
        outResult.vertices = new Vector3[netCubes.Count];
        outResult.normals = new Vector3[netCubes.Count];
        outResult.triangles = new int[edgesCrossingSurface.Count * 2 * 3];
        for (int i = 0; i < netCubes.Count; i++)
        {
            outResult.vertices[i] = netCubes[i].v;
        }
        // free memory
        netCubes = new List<GridCube>();
        int iTriangle = 0;
        for (int iEdge = 0; iEdge < edgesCrossingSurface.Count; iEdge++)
        {
            GridEdge edge = edgesCrossingSurface[iEdge];
            GridCoordinate c0 = edge.c0;
            GridCoordinate c1 = edge.c1;
            int[] winding;
            if (c0.i < c1.i || c0.j > c1.j || c0.k < c1.k)
            {
                winding = new int[3] { 1, 3, 2 };
            }
            else
            {
                winding = new int[3] { 2, 3, 1 };
            }
            for (int i = 0; i < 2; i++)
            {
                outResult.triangles[iTriangle++] = netEdgesCubes[4 * iEdge];
                outResult.triangles[iTriangle++] = netEdgesCubes[4 * iEdge + winding[i]];
                outResult.triangles[iTriangle++] = netEdgesCubes[4 * iEdge + winding[i + 1]];
            }
            currentProgress.SetProgress(iEdge / (double)(edgesCrossingSurface.Count + outResult.vertices.Length));
        }
        Debug.Assert(iTriangle == outResult.triangles.Length);
        int bufsz = computeThreads * shaderBatches;
        float[] input = new float[bufsz * RayContext.floatSize];
        float[] output = new float[bufsz * RayResult.floatSize];
        int bufferIdx = 0;
        for (int i = 0; i < outResult.vertices.Length; i++)
        {
            RayContext ctx;
            ctx.p = outResult.vertices[i];
            ctx.dir = Vector3.zero;
            ctx.WriteTo(input, bufferIdx++);
            if (bufferIdx == bufsz)
            {
                InvokeShader(input, output, bufferIdx, kernelDistanceMain);
                RayResult res = new RayResult();
                for (int j = 0; j < bufferIdx; j++)
                {
                    res.ReadFrom(output, j);
                    outResult.normals[i - bufferIdx + 1 + j] = res.n;
                }
                bufferIdx = 0;
                currentProgress.SetProgress((edgesCrossingSurface.Count + i + 1) / (double)(edgesCrossingSurface.Count + outResult.vertices.Length));
            }
        }
        if (bufferIdx > 0)
        {
            InvokeShader(input, output, bufferIdx, kernelDistanceMain);
            RayResult res = new RayResult();
            for (int j = 0; j < bufferIdx; j++)
            {
                res.ReadFrom(output, j);
                outResult.normals[outResult.normals.Length - bufferIdx + j] = res.n;
            }
        }
        currentProgress.SetProgress(1.0);
        currentStep = AlgorithmStep.Finished;
    }

    private Vector3 VectorFromIndices(int i, int j, int k)
    {
        Vector3 v;
        v.x = (float)(i) * (2f * gridRadius / gridSize) - gridRadius;
        v.y = (float)(j) * (2f * gridRadius / gridSize) - gridRadius;
        v.z = (float)(k) * (2f * gridRadius / gridSize) - gridRadius;
        return v;
    }

    private Vector3 VectorFromCoordinate(GridCoordinate c)
    {
        return VectorFromIndices(c.i, c.j, c.k);
    }

    private static GridCoordinate UnpackCubeVertex(int vertex)
    {
        return new GridCoordinate(vertex & 1, (vertex >> 1) & 1, (vertex >> 2) & 1);
    }
}