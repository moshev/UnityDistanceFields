using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public struct GridCoordinate : IComparable<GridCoordinate>
{
    public int i, j, k;

    public GridCoordinate(int i, int j, int k)
    {
        this.i = i;
        this.j = j;
        this.k = k;
    }

    public int CompareTo(GridCoordinate other)
    {
        return k != other.k ? k.CompareTo(other.k) :
               j != other.j ? j.CompareTo(other.j) :
                              i.CompareTo(other.i);
    }

    public static GridCoordinate operator +(GridCoordinate a, GridCoordinate b)
    {
        return new GridCoordinate(a.i + b.i, a.j + b.j, a.k + b.k);
    }

    public static GridCoordinate operator -(GridCoordinate a, GridCoordinate b)
    {
        return new GridCoordinate(a.i - b.i, a.j - b.j, a.k - b.k);
    }

    public static GridCoordinate operator *(GridCoordinate a, int b)
    {
        return new GridCoordinate(a.i * b, a.j * b, a.k * b);
    }

    public static GridCoordinate operator *(int a, GridCoordinate b)
    {
        return b * a;
    }

    public static GridCoordinate operator /(GridCoordinate a, int b)
    {
        return new GridCoordinate(a.i / b, a.j / b, a.k / b);
    }

    public static bool operator ==(GridCoordinate a, GridCoordinate b)
    {
        return a.i == b.i && a.j == b.j && a.k == b.k;
    }

    public static bool operator !=(GridCoordinate a, GridCoordinate b)
    {
        return !(a == b);
    }

    public Vector3 ToVector3()
    {
        return new Vector3(i, j, k);
    }

    public override string ToString()
    {
        return String.Format("[{0} {1} {2}]", i, j, k);
    }

    public override bool Equals(object obj)
    {
        if (obj is GridCoordinate)
        {
            return this == (GridCoordinate)obj;
        }
        else
        {
            return obj.Equals(this);
        }
    }

    public override int GetHashCode()
    {
        return i.GetHashCode() ^ j.GetHashCode() ^ k.GetHashCode();
    }
}

public struct GridEdge : IComparable<GridEdge>
{
    //! Negative vertex
    public GridCoordinate c0;

    //! Positive vertex
    public GridCoordinate c1;

    //! Distance field at v0
    public float d0;

    //! Distance field at v1
    public float d1;

    //! t*v0+(1-t)*v1 = crossing point
    public float t;

    public GridEdge(GridCoordinate c0, GridCoordinate c1, float d0, float d1)
    {
        this.c0 = c0;
        this.c1 = c1;
        this.d0 = d0;
        this.d1 = d1;
        t = (Math.Abs(d1 - d0) < 1e-6f) ? 0.5f : d1 / (d1 - d0);
    }

    public int CompareTo(GridEdge other)
    {
        int v0Cmp = c0.CompareTo(other.c0);
        return v0Cmp != 0 ? v0Cmp : c1.CompareTo(other.c1);
    }

    public override String ToString()
    {
        return c0 + "->" + c1;
    }
}

// Maintains associations between a GridCoordinate and a T
public class GridCoordinateOctree<T> : IEnumerable<T>
{
    private struct Element
    {
        public GridCoordinate c;
        public T v;
    }

    private interface INode : IEnumerable<T>
    {
        // Get value at coordinate c; return true if found; false otherwise
        bool Get(ref T v, GridCoordinate c);

        // Get node matching predicate (there can be multiple nodes per coordinate)
        bool Get(ref T v, GridCoordinate c, Predicate<T> predicate);

        // Add v at c; return a new INode if split was necessary
        INode Add(T v, GridCoordinate c);

        // Transform each value in-place
        void Map(Func<T, T> func);
    }

    private class LeafNode : INode
    {
        private Element[] elements;// = new Element[8];
        private int count = 0;
        private GridCoordinate min;
        private GridCoordinate max;

        public LeafNode(GridCoordinate min, GridCoordinate max)
        {
            this.min = min;
            this.max = max;
        }

        public bool Get(ref T v, GridCoordinate c)
        {
            for (int i = 0; i < count; i++)
            {
                if (elements[i].c == c)
                {
                    v = elements[i].v;
                    return true;
                }
            }
            return false;
        }

        public bool Get(ref T v, GridCoordinate c, Predicate<T> predicate)
        {
            for (int i = 0; i < count; i++)
            {
                if (elements[i].c == c && predicate(elements[i].v))
                {
                    v = elements[i].v;
                    return true;
                }
            }
            return false;
        }

        public INode Add(T v, GridCoordinate c)
        {
            if (count == 0) elements = new Element[8];
            if (count < elements.Length)
            {
                Element e = new Element();
                e.c = c;
                e.v = v;
                elements[count++] = e;
                return this;
            }
            else
            {
                GridCoordinate diff = (max - min);
                if (diff.i < 2 && diff.j < 2 && diff.k < 2)
                {
                    // in this case we'll have nodes that are just too small
                    Debug.Log("Error trying to split node! Size is " + diff);
                    return this;
                }
                INode split = new ParentNode(min, max);
                for (int i = 0; i < elements.Length; i++)
                {
                    Element e = elements[i];
                    split = split.Add(e.v, e.c);
                }
                return split.Add(v, c);
            }
        }

        public void Map(Func<T, T> func)
        {
            for (int i = 0; i < count; i++)
            {
                elements[i].v = func(elements[i].v);
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            for (int i = 0; i < count; i++)
            {
                yield return elements[i].v;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    private class ParentNode : INode
    {
        private INode[] children = new INode[8];
        private GridCoordinate min;
        private GridCoordinate max;

        public ParentNode(GridCoordinate min, GridCoordinate max)
        {
            this.min = min;
            this.max = max;
        }

        public bool Get(ref T v, GridCoordinate c)
        {
            GridCoordinate mid = (min + max) / 2;
            int n = 0;
            if (c.i >= mid.i) n |= 1;
            if (c.j >= mid.j) n |= 2;
            if (c.k >= mid.k) n |= 4;
            if (children[n] == null)
            {
                return false;
            }
            return children[n].Get(ref v, c);
        }

        public bool Get(ref T v, GridCoordinate c, Predicate<T> predicate)
        {
            GridCoordinate mid = (min + max) / 2;
            int n = 0;
            if (c.i >= mid.i) n |= 1;
            if (c.j >= mid.j) n |= 2;
            if (c.k >= mid.k) n |= 4;
            if (children[n] == null)
            {
                return false;
            }
            return children[n].Get(ref v, c, predicate);
        }

        public INode Add(T v, GridCoordinate c)
        {
            GridCoordinate mid = (min + max) / 2;
            int n = 0;
            if (c.i >= mid.i) n |= 1;
            if (c.j >= mid.j) n |= 2;
            if (c.k >= mid.k) n |= 4;
            if (children[n] == null)
            {
                GridCoordinate[] corners = new GridCoordinate[3] { min, mid, max };
                GridCoordinate nmin, nmax;
                nmin.i = corners[(n & 1) / 1 + 0].i;
                nmax.i = corners[(n & 1) / 1 + 1].i;
                nmin.j = corners[(n & 2) / 2 + 0].j;
                nmax.j = corners[(n & 2) / 2 + 1].j;
                nmin.k = corners[(n & 4) / 4 + 0].k;
                nmax.k = corners[(n & 4) / 4 + 1].k;
                children[n] = new LeafNode(nmin, nmax);
            }
            children[n] = children[n].Add(v, c);
            return this;
        }

        public void Map(Func<T, T> func)
        {
            for (int n = 0; n < 8; n++)
            {
                if (children[n] == null) continue;
                children[n].Map(func);
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            for (int n = 0; n < 8; n++)
            {
                if (children[n] == null) continue;
                foreach (T v in children[n])
                {
                    yield return v;
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    private int count = 0;
    private INode rootNode;
    public int Count { get { return count; } }

    public GridCoordinateOctree(GridCoordinate min, GridCoordinate max)
    {
        rootNode = new LeafNode(min, max);
    }

    // Get value at coordinate c; return true if found; false otherwise
    public bool Get(ref T v, GridCoordinate c)
    {
        return rootNode.Get(ref v, c);
    }

    // Get node matching predicate (there can be multiple nodes per coordinate)
    public bool Get(ref T v, GridCoordinate c, Predicate<T> predicate)
    {
        return rootNode.Get(ref v, c, predicate);
    }

    // Add v at c; return a new INode if split was necessary
    public void Add(T v, GridCoordinate c)
    {
        rootNode = rootNode.Add(v, c);
        count++;
    }

    // Transform each value in-place
    public void Map(Func<T, T> func)
    {
        rootNode.Map(func);
    }

    public IEnumerator<T> GetEnumerator()
    {
        foreach (T v in rootNode)
        {
            yield return v;
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}

public struct IndexedVector3
{
    public Vector3 v;
    public int i;

    public static IndexedVector3 Create(Vector3 v)
    {
        IndexedVector3 result;
        result.v = v;
        result.i = -1;
        return result;
    }
}

[ExecuteInEditMode]
public abstract class DEBase : MonoBehaviour
{
    protected abstract float Distance(Vector3 p);

    public Vector3 Gradient(Vector3 centre, float epsilon = 0.0001f)
    {
        Vector3 gradient;
        gradient.x = Distance(centre + epsilon * Vector3.right) - Distance(centre - epsilon * Vector3.right);
        gradient.y = Distance(centre + epsilon * Vector3.up) - Distance(centre - epsilon * Vector3.up);
        gradient.z = Distance(centre + epsilon * Vector3.forward) - Distance(centre - epsilon * Vector3.forward);
        return gradient;
    }

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

    [Range(0.5f, 128)]
    public float gridRadius = 8.0f;

    [Range(4, 256)]
    public int gridSize = 48;

    public bool showErrorEdges = false;

    public bool debugAlwaysShowEdges = false;

    private float cornerScale;

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

    public void AlgorithmClear()
    {
        MeshFilter mf = GetComponent<MeshFilter>();
        mf.mesh = null;
        currentStep = AlgorithmStep.NotStarted;
        cornerScale = 2.0f * gridRadius / gridSize;
        //cornersGridSize = subdivs + 1;
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
        StartTask(progressReport, "Calculating distances", TaskCalculateDistances);
    }

    private void TaskCalculateDistances()
    {
        Debug.Assert(currentProgress != null);
        distances = new float[gridSize, gridSize, gridSize];
        for (int k = 0; k < gridSize; k++)
        {
            for (int j = 0; j < gridSize; j++)
            {
                for (int i = 0; i < gridSize; i++)
                {
                    Vector3 centre = VectorFromIndices(i, j, k);
                    distances[i, j, k] = Distance(centre);
                }
            }
            currentProgress.SetProgress((k + 1) / (double)gridSize);
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
                        float d = Distance(p);
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

    public void AlgorithmCreateMesh(ProgressReport progressReport)
    {
        if (netVertices.Count > 65000)
        {
            Debug.Log(String.Format("Refusing to create mesh with more than 65k vertices: {0}", netVertices.Count));
            return;
        }
        MeshFilter mf = GetComponent<MeshFilter>();
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
            normals[i] = Gradient(vertices[i]);
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

    private void OnDrawGizmosSelected()
    {
        switch (currentStep)
        {
            case AlgorithmStep.NotStarted:
                break;

            case AlgorithmStep.DistanceCalculated:
                DrawDistanceGizmos();
                break;

            case AlgorithmStep.EdgeIntersectionsFound:
                DrawEdgeGizmos();
                break;

            case AlgorithmStep.VerticesConstructed:
                DrawEdgeGizmos();
                DrawVertexGizmos();
                break;

            case AlgorithmStep.Finished:
                if (debugAlwaysShowEdges) DrawEdgeGizmos();
                break;
        }
    }

    public void DrawDistanceGizmos()
    {
        for (int k = 0; k < gridSize; k++)
        {
            for (int j = 0; j < gridSize; j++)
            {
                for (int i = 0; i < gridSize; i++)
                {
                    Vector3 centre = VectorFromIndices(i, j, k);
                    float d = distances[i, j, k];
                    if (d > 0)
                    {
                        //Gizmos.color = new Color(0.5f, 0.5f, 0.5f, 1f);
                        continue;
                    }
                    else
                    {
                        float red = (float)Math.Pow(2, d);
                        Gizmos.color = new Color(red, 1 - red, 0, 1f);
                    }
                    float size = 0.5f;// (float)Math.Pow(2, -2 - Math.Abs(d));
                    if (size < 0.1f) continue;
                    if (d > 0)
                    {
                        Gizmos.DrawWireCube(centre + transform.position, cornerScale * size * Vector3.one);
                    }
                    else
                    {
                        Gizmos.DrawCube(centre + transform.position, cornerScale * size * Vector3.one);
                    }
                }
            }
        }
    }

    public void DrawEdgeGizmos()
    {
        foreach (GridEdge e in edgesCrossingSurface)
        {
            Vector3 v0 = VectorFromCoordinate(e.c0) + transform.position;
            Vector3 v1 = VectorFromCoordinate(e.c1) + transform.position;
            Vector3 vmid = e.t * v0 + (1f - e.t) * v1;
            Gizmos.color = Color.green;
            Gizmos.DrawLine(v0, vmid);
            Gizmos.color = Color.red;
            Gizmos.DrawLine(vmid, v1);
        }
        if (errorEdges == null || !showErrorEdges) return;
        Gizmos.color = new Color(0.2f, 0.5f, 0.3f);
        foreach (GridEdge e in errorEdges)
        {
            Vector3 v0 = VectorFromCoordinate(e.c0) + transform.position;
            Vector3 v1 = VectorFromCoordinate(e.c1) + transform.position;
            Gizmos.DrawLine(v0, v1);
        }
    }

    public void DrawVertexGizmos()
    {
        float size = 0.2f;
        Gizmos.color = Color.black;
        foreach (IndexedVector3 iv in netVertices)
        {
            Vector3 pos = iv.v + transform.position;
            Gizmos.DrawCube(pos, size * cornerScale * Vector3.one);
        }
    }

    private void Start()
    {
        AlgorithmClear();
        //UpdateMesh();
    }
}