using System;
using System.Collections.Generic;
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

    public const int floatSize = 7;
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
    private const int shaderBatches = 16;

    private int kernelIndex;

    private ComputeBuffer computeInput;
    private ComputeBuffer computeOutput;

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
    private GridCoordinateOctree<GridEdge> edgesCrossingSurface;
    private GridCoordinateOctree<IndexedVector3> netVertices;

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

    public void InitBuffers()
    {
        int bufsz = computeThreads * shaderBatches;
        computeInput = new ComputeBuffer(bufsz, RayContext.floatSize * sizeof(float));
        computeOutput = new ComputeBuffer(bufsz, RayResult.floatSize * sizeof(float));
    }

    public void InitKernel()
    {
        kernelIndex = distanceEstimator.FindKernel("DistanceMain");
        if (kernelIndex < 0)
        {
            throw new Exception("Can't find kernel DistanceMain");
        }
        distanceEstimator.SetBuffer(kernelIndex, "_input", computeInput);
        distanceEstimator.SetBuffer(kernelIndex, "_output", computeOutput);
        rootNode.SetTransformsInComputeShader(distanceEstimator, true);
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

    private void StartTask(ProgressReport progressReport, string message, ThreadStart task)
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

    public void AlgorithmCalculateDistances(ProgressReport progressReport)
    {
        currentProgress = progressReport;
        currentProgress.StartProgress("Calculating distances");
        TaskCalculateDistances();
        progressReport.EndProgress();
        //StartTask(progressReport, "Calculating distances", TaskCalculateDistances);
        Debug.LogAssertion("GUI thread: Distance Estimator is null: " + (distanceEstimator == null));
    }

    private void InvokeShader(ComputeBuffer input, ComputeBuffer output, float[] vinput, int nInputs)
    {
        for (int i = nInputs * RayContext.floatSize; i < vinput.Length; i++)
        {
            vinput[i] = 0;
        }
        input.SetData(vinput);
        int tgroups = (nInputs + computeThreads - 1) / computeThreads;
        Debug.Log("Dispatching " + tgroups + " thread groups");
        distanceEstimator.Dispatch(kernelIndex, tgroups, 1, 1);
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
                        Debug.Log(String.Format("Distance {0} {1} {2} = {3}", i, j, k, res.distance));
                    }
                }
                _i1 = 0;
            }
            _j1 = 0;
        }
    }

    private void TaskCalculateDistances()
    {
        Debug.Assert(currentProgress != null);
        Debug.LogAssertion("Task thread: Distance Estimator is null: " + (distanceEstimator == null));
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
                        Debug.Log(String.Format("World position for {0} {1} {2} is {3} {4} {5}", i, j, k, ctx.p.x, ctx.p.y, ctx.p.z));
                    }
                    ctx.dir = Vector3.zero;
                    ctx.WriteTo(input, bufferIdx++);
                    if (bufferIdx == bufsz)
                    {
                        InvokeShader(computeInput, computeOutput, input, bufsz);
                        computeOutput.GetData(output);
                        //Debug.Log(String.Format("Filling from {0} {1} {2} to {3} {4} {5} - {6} elements", iStart, jStart, kStart, i, j, k, bufsz));
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
            InvokeShader(computeInput, computeOutput, input, bufferIdx);
            computeOutput.GetData(output);
            FillDistances(output, iStart, jStart, kStart, gridSize - 1, gridSize - 1, gridSize - 1);
        }
        currentStep = AlgorithmStep.DistanceCalculated;
    }

    public void AlgorithmFindEdgeIntersections(ProgressReport progressReport)
    {
        StartTask(progressReport, "Finding edge intersections", TaskFindEdgeIntersections);
    }

    public void TaskFindEdgeIntersections()
    {
        Debug.Assert(currentProgress != null);
        edgesCrossingSurface = new GridCoordinateOctree<GridEdge>(
            new GridCoordinate(0, 0, 0), new GridCoordinate(gridSize, gridSize, gridSize));
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
                        Vector3 p = t * v0 + (1 - t) * v1;
                        // XXX comment for compile
                        float d = 0;// Distance(p);
                        for (int q = 0; q < 4 && Math.Abs(d) > 0.0001; q++)
                        {
                            t += cornerScale * d;
                            p = t * v0 + (1 - t) * v1;
                        }
                        e.t = t;
                        edgesCrossingSurface.Add(e, e.c0);
                    }
                }
            }
            currentProgress.SetProgress((k + 1) / (double)gridSize);
        }
        currentStep = AlgorithmStep.EdgeIntersectionsFound;
    }

    private static int[] voxelEdgeVertex0 = new int[]
    {
        0, 0, 0, 5, 5, 5, 3, 3, 3, 6, 6, 6,
    };

    private static int[] voxelEdgeVertex1 = new int[]
    {
        1, 2, 4, 1, 7, 4, 1, 2, 7, 2, 4, 7,
    };

    private List<GridEdge> errorEdges;

    public void AlgorithmConstructVertices(ProgressReport progressReport)
    {
        StartTask(progressReport, "Constructing vertices", TaskConstructVertices);
    }

    public void TaskConstructVertices()
    {
        netVertices = new GridCoordinateOctree<IndexedVector3>(
            new GridCoordinate(0, 0, 0), new GridCoordinate(gridSize, gridSize, gridSize));
        errorEdges = new List<GridEdge>();
        int errors = 0;
        for (int k = 0; k < gridSize - 1; k++)
        {
            for (int j = 0; j < gridSize - 1; j++)
            {
                for (int i = 0; i < gridSize - 1; i++)
                {
                    int nEdges = 0;
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
                        sum += t * v0 + (1 - t) * v1;
                        nEdges++;
                    }
                    if (nEdges > 0)
                    {
                        netVertices.Add(IndexedVector3.Create(sum / nEdges), cBase);
                        //sum = VectorFromCoordinate(cBase);
                        //meshVertices.Add(sum + 0.5f * cornerScale * Vector3.one);
                    }
                }
            }
            currentProgress.SetProgress((k + 1) / (double)(gridSize - 1));
        }
        Debug.Log("Total error edges " + errors);
        currentStep = AlgorithmStep.VerticesConstructed;
    }

    private class NetToArrayMapper
    {
        private Vector3[] vertices;
        private int iVertex;

        public NetToArrayMapper(Vector3[] vertices)
        {
            this.vertices = vertices;
            iVertex = 0;
        }

        public IndexedVector3 Put(IndexedVector3 iv)
        {
            iv.i = iVertex++;
            vertices[iv.i] = iv.v;
            return iv;
        }
    }

    private class CreateMeshResult
    {
        public volatile Vector3[] vertices;
        public volatile Vector3[] normals;
        public volatile int[] triangles;
    }

    public void AlgorithmCreateMesh(ProgressReport progressReport, MeshFilter mf)
    {
        if (netVertices.Count > 65000)
        {
            Debug.Log(String.Format("Refusing to create mesh with more than 65k vertices: {0}", netVertices.Count));
            return;
        }
        if (mf == null)
        {
            Debug.Log("No mesh filter assigned!");
            return;
        }
        mf.mesh = null;
        CreateMeshResult result = new CreateMeshResult();
        progressReport.Callback = delegate ()
        {
            Debug.Log(String.Format("Assigning mesh with {0} vertices and {1} triangles",
                result.vertices.Length, result.triangles.Length));
            Mesh mesh = new Mesh();
            mesh.vertices = result.vertices;
            //mesh.normals = result.normals;
            mesh.triangles = result.triangles;
            mesh.RecalculateNormals();
            mf.mesh = mesh;
        };
        StartTask(progressReport, "Creating mesh", () => TaskCreateMesh(result));
    }

    private void TaskCreateMesh(CreateMeshResult outResult)
    {
        int[] triangles = new int[edgesCrossingSurface.Count * 2 * 3];
        Vector3[] vertices = new Vector3[netVertices.Count];
        netVertices.Map(new NetToArrayMapper(vertices).Put);
        int iTriangle = 0;
        int iEdge = 0;
        foreach (GridEdge edge in edgesCrossingSurface)
        {
            GridCoordinate c0 = edge.c0;
            GridCoordinate c1 = edge.c1;
            Debug.Assert(c0.CompareTo(c1) != 0);
            //Vector3 vmid = edge.t * VectorFromCoordinate(edge.c0) + (1 - edge.t) * VectorFromCoordinate(edge.c1);
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
                for (int j = cBase.j - dj; j <= cBase.j; j++)
                {
                    for (int i = cBase.i - di; i <= cBase.i; i++)
                    {
                        IndexedVector3 iv = new IndexedVector3();
                        bool found = netVertices.Get(ref iv, new GridCoordinate(i, j, k));
                        if (!found)
                        {
                            break;
                        }
                        indices[iidx++] = iv.i;
                    }
                }
            }
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
                triangles[iTriangle++] = indices[0];
                triangles[iTriangle++] = indices[winding[i]];
                triangles[iTriangle++] = indices[winding[i + 1]];
            }
            iEdge++;
            currentProgress.SetProgress(iEdge / (double)(edgesCrossingSurface.Count + vertices.Length));
        }
        Debug.Assert(iTriangle == triangles.Length);
        Vector3[] normals = new Vector3[vertices.Length];
        for (int i = 0; i < vertices.Length; i++)
        {
            // XXX comment for compile
            //normals[i] = Gradient(vertices[i]);
            normals[i] = Vector3.up;
            currentProgress.SetProgress((iEdge + i + 1) / (double)(edgesCrossingSurface.Count + vertices.Length));
        }
        outResult.vertices = vertices;
        outResult.normals = normals;
        outResult.triangles = triangles;
        currentStep = AlgorithmStep.Finished;
    }

    private static bool FindEdgeIntersection(out Vector3 intersection, float d0, float d1, Vector3 e0, Vector3 e1)
    {
        intersection = Vector3.zero;
        if (d0 * d1 < 0)
        {
            return false;
        }
        float t = (Math.Abs(d1 - d0) < 1e-6f) ? 0.5f : d1 / (d1 - d0);
        intersection = t * e0 + (1f - t) * e1;
        return true;
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