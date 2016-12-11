using System;
using System.Collections;
using System.Collections.Generic;
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

public class VolumeGrid : IEnumerable<Vector3>
{
    private int gridSize;
    private Vector3[][][] grid;
    private bool[][][] present;

    public VolumeGrid(int gridSize)
    {
        this.gridSize = gridSize;
        this.grid = new Vector3[gridSize][][];
        this.present = new bool[gridSize][][];
    }

    public static void Set<T>(T[][][] level1, GridCoordinate c, T value)
    {
        int gridSize = level1.Length;
        T[][] level2 = level1[c.k];
        if (level2 == null)
        {
            level2 = new T[gridSize][];
            level1[c.k] = level2;
        }
        T[] level3 = level2[c.j];
        if (level3 == null)
        {
            level3 = new T[gridSize];
            level2[c.j] = level3;
        }
        level3[c.i] = value;
    }

    public static T Get<T>(T[][][] level1, GridCoordinate c) where T : new()
    {
        T zero = new T();
        T[][] level2 = level1[c.k];
        if (level2 == null) return zero;
        T[] level3 = level2[c.j];
        if (level3 == null) return zero;
        return level3[c.i];
    }

    public void Add(GridCoordinate c, Vector3 v)
    {
        try
        {
            Set(grid, c, v);
            Set(present, c, true);
        }
        catch (ArgumentOutOfRangeException e)
        {
            Debug.Log("s-h-i-t");
            throw e;
        }
    }

    public bool Get(out Vector3 v, GridCoordinate c)
    {
        v = Get(grid, c);
        return Get(present, c);
    }

    public IEnumerator<Vector3> GetEnumerator()
    {
        for (int k = 0; k < gridSize; k++)
        {
            Vector3[][] gLevel2 = grid[k];
            bool[][] pLevel2 = present[k];
            if (gLevel2 == null || pLevel2 == null) continue;
            for (int j = 0; j < gridSize; j++)
            {
                Vector3[] gLevel3 = gLevel2[j];
                bool[] pLevel3 = pLevel2[j];
                if (gLevel3 == null || pLevel3 == null) continue;
                for (int i = 0; i < gridSize; i++)
                {
                    if (!pLevel3[i]) continue;
                    yield return gLevel3[i];
                }
            }
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}

// Maintains associations between a GridCoordinate and a T
public class GridCoordinateOctree<T> : IEnumerable<T> where T : new()
{
    private struct Element
    {
        public GridCoordinate c;
        public T v;
    }

    private interface INode : IEnumerable<T>
    {
        // Get value at coordinate c; return true if found; false otherwise
        bool Get(out T v, GridCoordinate c);

        // Get node matching predicate (there can be multiple nodes per coordinate)
        bool Get(out T v, GridCoordinate c, Predicate<T> predicate);

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

        public bool Get(out T v, GridCoordinate c)
        {
            for (int i = 0; i < count; i++)
            {
                if (elements[i].c == c)
                {
                    v = elements[i].v;
                    return true;
                }
            }
            v = new T();
            return false;
        }

        public bool Get(out T v, GridCoordinate c, Predicate<T> predicate)
        {
            for (int i = 0; i < count; i++)
            {
                if (elements[i].c == c && predicate(elements[i].v))
                {
                    v = elements[i].v;
                    return true;
                }
            }
            v = new T();
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

        public bool Get(out T v, GridCoordinate c)
        {
            GridCoordinate mid = (min + max) / 2;
            int n = 0;
            if (c.i >= mid.i) n |= 1;
            if (c.j >= mid.j) n |= 2;
            if (c.k >= mid.k) n |= 4;
            if (children[n] == null)
            {
                v = new T();
                return false;
            }
            return children[n].Get(out v, c);
        }

        public bool Get(out T v, GridCoordinate c, Predicate<T> predicate)
        {
            GridCoordinate mid = (min + max) / 2;
            int n = 0;
            if (c.i >= mid.i) n |= 1;
            if (c.j >= mid.j) n |= 2;
            if (c.k >= mid.k) n |= 4;
            if (children[n] == null)
            {
                v = new T();
                return false;
            }
            return children[n].Get(out v, c, predicate);
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
    public bool Get(out T v, GridCoordinate c)
    {
        return rootNode.Get(out v, c);
    }

    // Get node matching predicate (there can be multiple nodes per coordinate)
    public bool Get(out T v, GridCoordinate c, Predicate<T> predicate)
    {
        return rootNode.Get(out v, c, predicate);
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

    //private int cornersGridSize;
    private MeshFilter meshFilter;

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

    public void AlgorithmCalculateDistances()
    {
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
        }
        currentStep = AlgorithmStep.DistanceCalculated;
    }

    public void AlgorithmFindEdgeIntersections()
    {
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

    public void AlgorithmConstructVertices()
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
                        GridEdge gridEdge;
                        bool found = edgesCrossingSurface.Get(out gridEdge, c0,
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

    public void AlgorithmCreateMesh()
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
        Mesh mesh = new Mesh();
        int[] triangles = new int[edgesCrossingSurface.Count * 2 * 3];
        Vector3[] vertices = new Vector3[netVertices.Count];
        netVertices.Map(new NetToArrayMapper(vertices).Put);
        int iTriangle = 0;
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
                        IndexedVector3 iv;
                        bool found = netVertices.Get(out iv, new GridCoordinate(i, j, k));
                        if (!found)
                        {
                            break;
                        }
                        indices[iidx++] = iv.i;
                    }
                }
            }
            /*
            int[] winding;
            if (c0.i < c1.i || c0.j > c1.j || c0.k < c1.k)
            {
                winding = new int[5] { 0, 1, 3, 2, 0 };
            }
            else
            {
                winding = new int[5] { 1, 0, 2, 3, 1 };
            }
            for (int i = 0; i < 4; i++)
            {
                triangles[iTriangle++] = iStartVertex + winding[i];
                triangles[iTriangle++] = iStartVertex + winding[i + 1];
                triangles[iTriangle++] = iStartVertex + 4;
            }
            */
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
        }
        if (iTriangle != triangles.Length)
        {
            //Debug.Break();
            int[] t2 = new int[iTriangle];
            Array.Copy(triangles, t2, iTriangle);
            triangles = t2;
        }
        Vector3[] normals = new Vector3[vertices.Length];
        for (int i = 0; i < vertices.Length; i++)
        {
            normals[i] = Gradient(vertices[i]);
        }
        Debug.Log(String.Format("Assigning mesh with {0} vertices and {1} triangles", vertices.Length, triangles.Length));
        mesh.vertices = vertices;
        //mesh.normals = normals;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mf.mesh = mesh;
        currentStep = AlgorithmStep.Finished;
        /*
        int[,,] cornerVertexIndices = new int[cornersGridSize, cornersGridSize, cornersGridSize];
        bool[,,] addCube = new bool[subdivs, subdivs, subdivs];
        System.Array.Clear(cornerVertexIndices, 0, cornerVertexIndices.Length);
        System.Array.Clear(addCube, 0, addCube.Length);
        for (int k = 0; k < subdivs; k++)
        {
            for (int j = 0; j < subdivs; j++)
            {
                for (int i = 0; i < subdivs; i++)
                {
                    // only interested on cubes inside
                    if (distances[i + 1, j + 1, k + 1] > 0) continue;
                    Vector3 centre;
                    centre.x = (float)i * (2 * gridRadius / subdivs) - gridRadius;
                    centre.y = (float)j * (2 * gridRadius / subdivs) - gridRadius;
                    centre.z = (float)k * (2 * gridRadius / subdivs) - gridRadius;
                    int firstVertexIdx = vertices.Count;
                    int firstTriangleIdx = triangles.Count;
                    for (int iQuad = 0; iQuad < 6; iQuad++)
                    {
                        int iCorner = iQuad * 4;
                        Vector3 d =
                            corners[quadCorners[iCorner + 0]] +
                            corners[quadCorners[iCorner + 1]] +
                            corners[quadCorners[iCorner + 2]] +
                            corners[quadCorners[iCorner + 3]];
                        int di = d.x < -0.5 ? -1 : d.x > 0.5 ? 1 : 0;
                        int dj = d.y < -0.5 ? -1 : d.y > 0.5 ? 1 : 0;
                        int dk = d.z < -0.5 ? -1 : d.z > 0.5 ? 1 : 0;
                        Vector3 edgeDirection = new Vector3(di, dj, dk);
                        if (distances[i + 1 + di, j + 1 + dj, k + 1 + dk] < 0) continue;
                        // 9 edges total surround the 4 vertices 3x3 grid
                        // for each one find the intersection with the surface
                        Vector3[,] edges = new Vector3[3, 3];
                        // if the edge crosses the surface
                        bool[,] edgeCrosses = new bool[3, 3];
                        // Check edges in X direction
                        if (di != 0)
                        {
                            Debug.Assert(dj == 0 && dk == 0);
                            for (int dkE = 0; dkE < 3; dkE++)
                            {
                                for (int djE = 0; djE < 3; djE++)
                                {
                                    float d0 = distances[i + 1, j + djE, k + dkE];
                                    float d1 = distances[i + 1 + di, j + djE, k + dkE];
                                    Vector3 e0 = centre + cornerScale * new Vector3(0, djE - 1, dkE - 1);
                                    Vector3 e1 = centre + cornerScale * new Vector3(di, djE - 1, dkE - 1);
                                    edgeCrosses[djE, dkE] = FindEdgeIntersection(out edges[djE, dkE], d0, d1, e0, e1);
                                }
                            }
                        }
                        // Check edges in Y direction
                        else if (dj != 0)
                        {
                            Debug.Assert(di == 0 && dk == 0);
                            for (int dkE = 0; dkE < 3; dkE++)
                            {
                                for (int diE = 0; diE < 3; diE++)
                                {
                                    float d0 = distances[i + diE, j + 1, k + dkE];
                                    float d1 = distances[i + diE, j + 1 + dj, k + dkE];
                                    Vector3 e0 = centre + cornerScale * new Vector3(diE - 1, 0, dkE - 1);
                                    Vector3 e1 = centre + cornerScale * new Vector3(diE - 1, dj, dkE - 1);
                                    edgeCrosses[diE, dkE] = FindEdgeIntersection(out edges[diE, dkE], d0, d1, e0, e1);
                                }
                            }
                        }
                        // Check edges in Z direction
                        else if (dk != 0)
                        {
                            Debug.Assert(di == 0 && dj == 0);
                            for (int djE = 0; djE < 3; djE++)
                            {
                                for (int diE = 0; diE < 3; diE++)
                                {
                                    float d0 = distances[i + diE, j + djE, k + 1];
                                    float d1 = distances[i + diE, j + djE, k + 1 + dk];
                                    Vector3 e0 = centre + cornerScale * new Vector3(diE - 1, djE - 1, 0);
                                    Vector3 e1 = centre + cornerScale * new Vector3(diE - 1, djE - 1, dk);
                                    edgeCrosses[diE, djE] = FindEdgeIntersection(out edges[diE, djE], d0, d1, e0, e1);
                                }
                            }
                        }
                        else
                        {
                            throw new UnityException("Quad not situated on edge!");
                        }
                        // indices of the face's vertices in the vertices list
                        int[] indices = new int[4];
                        for (int iVertex = 0; iVertex < 4; iVertex++)
                        {
                            int vdi = corners[quadCorners[iCorner + iVertex]].x < 0 ? 0 : 1;
                            int vdj = corners[quadCorners[iCorner + iVertex]].y < 0 ? 0 : 1;
                            int vdk = corners[quadCorners[iCorner + iVertex]].z < 0 ? 0 : 1;
                            if (cornerVertexIndices[i + vdi, j + vdj, k + vdk] > 0)
                            {
                                indices[iVertex] = cornerVertexIndices[i + vdi, j + vdj, k + vdk] - 1;
                            }
                            else
                            {
                                int dim1Start, dim2Start;
                                if (di != 0)
                                {
                                    dim1Start = vdj;
                                    dim2Start = vdk;
                                }
                                else if (dj != 0)
                                {
                                    dim1Start = vdi;
                                    dim2Start = vdk;
                                }
                                else if (dk != 0)
                                {
                                    dim1Start = vdi;
                                    dim2Start = vdj;
                                }
                                else
                                {
                                    throw new UnityException("Vertex belongs to quad not situated on edge (should never happen)");
                                }
                                Vector3 sum = Vector3.zero;
                                int nEdges = 0;
                                for (int dim2 = 0; dim2 < 2; dim2++)
                                {
                                    for (int dim1 = 0; dim1 < 2; dim1++)
                                    {
                                        int a = dim1 + dim1Start;
                                        if (!edgeCrosses[dim1, dim2]) continue;
                                        nEdges++;
                                        sum += edges[dim1, dim2];
                                    }
                                }
                                if (nEdges == 0)
                                {
                                    vertices.Add(centre + cornerScale * corners[quadCorners[iCorner + iVertex]]);
                                }
                                else
                                {
                                    vertices.Add(sum / nEdges);
                                }
                                cornerVertexIndices[i + vdi, j + vdj, k + vdk] = firstVertexIdx + 1;
                                indices[iVertex] = firstVertexIdx;
                                firstVertexIdx++;
                            }
                        }
                        triangles.Add(indices[0]);
                        triangles.Add(indices[1]);
                        triangles.Add(indices[2]);
                        triangles.Add(indices[0]);
                        triangles.Add(indices[2]);
                        triangles.Add(indices[3]);
                    }
                }
            }
        }
        // do one round of smoothing, plus calculate normals
        List<Vector3> normals = new List<Vector3>(vertices.Count);
        for (int i = 0; i < vertices.Count; i++)
        {
            Vector3 normal = Gradient(vertices[i]).normalized;
            //vertices[i] -= Distance(vertices[i]) * normal;
            normals.Add(normal);
        }

        Debug.Log(string.Format("Created mesh with {0} vertices and {1} indices ({2} triangles)", vertices.Count, triangles.Count, triangles.Count / 3));

        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.normals = normals.ToArray();
        mf.mesh = mesh;
        */
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