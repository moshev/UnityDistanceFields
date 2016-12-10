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

    public Vector3 ToVector3()
    {
        return new Vector3(i, j, k);
    }

    public override string ToString()
    {
        return String.Format("[{0} {1} {2}]", i, j, k);
    }
}

public struct GridEdge : IComparable<GridEdge>
{
    //! Negative vertex
    public GridCoordinate v0;

    //! Positive vertex
    public GridCoordinate v1;

    //! Distance field at v0
    public float d0;

    //! Distance field at v1
    public float d1;

    //! t*v0+(1-t)*v1 = crossing point
    public float t;

    public GridEdge(GridCoordinate v0, GridCoordinate v1, float d0, float d1)
    {
        this.v0 = v0;
        this.v1 = v1;
        this.d0 = d0;
        this.d1 = d1;
        t = (Math.Abs(d1 - d0) < 1e-6f) ? 0.5f : d1 / (d1 - d0);
    }

    public int CompareTo(GridEdge other)
    {
        int v0Cmp = v0.CompareTo(other.v0);
        return v0Cmp != 0 ? v0Cmp : v1.CompareTo(other.v1);
    }

    public override String ToString()
    {
        return v0 + "->" + v1;
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

[ExecuteInEditMode]
public abstract class DEBase : MonoBehaviour
{
    protected abstract float Distance(Vector3 p);

    /*
    protected struct Edge
    {
        public int otherFace;
    };

    protected struct Face
    {
        public int[] vertices;
        public Edge[] edges;

        public static Face create(int nvertices)
        {
            Face f;
            f.vertices = new int[nvertices];
            f.edges = new Edge[nvertices];
            return f;
        }
    };

    protected class ConstructibleMesh
    {
        private List<Vector3> vertices = new List<Vector3>();
        private List<Face> faces = new List<Face>();
        private Dictionary<int, List<Edge>> edgesByVertex = new Dictionary<int, List<Edge>>();
        private Vector3 unscaledCenter = Vector3.zero;

        public Vector3 Center
        {
            get
            {
                Vector3 result = Vector3.zero;
                for (int i = 0; i < vertices.Count; i++)
                {
                    result += vertices[i];
                }
                return result / vertices.Count;
            }
        }

        private ConstructibleMesh()
        {
        }

        public static ConstructibleMesh CreateQuad()
        {
            ConstructibleMesh mesh = new ConstructibleMesh();
            mesh.vertices.Add(new Vector3(-1, -1, 0));
            mesh.vertices.Add(new Vector3(1, -1, 0));
            mesh.vertices.Add(new Vector3(1, 1, 0));
            mesh.vertices.Add(new Vector3(-1, 1, 0));
            Face f = Face.create(4);
            f.vertices[0] = 0;
            f.vertices[1] = 1;
            f.vertices[2] = 2;
            f.vertices[3] = 3;
            f.edges[0].otherFace = -1;
            f.edges[1].otherFace = -1;
            f.edges[2].otherFace = -1;
            f.edges[3].otherFace = -1;
            mesh.faces.Add(f);
            return mesh;
        }

        public Mesh ToMesh()
        {
            Mesh mesh = new Mesh();
            List<int> triangles = new List<int>();
            for (int i = 0; i < faces.Count; i++)
            {
                Face f = faces[i];
                int v1 = f.vertices[0];
                for (int j = 1; j < f.vertices.Length - 1; j++)
                {
                    int v2 = f.vertices[j];
                    int v3 = f.vertices[j + 1];
                    triangles.Add(v1);
                    triangles.Add(v2);
                    triangles.Add(v3);
                }
            }
            mesh.vertices = vertices.ToArray();
            mesh.triangles = triangles.ToArray();
            return mesh;
        }

        public void Scale(float factor)
        {
            for (int i = 0; i < vertices.Count; i++)
            {
                vertices[i] *= factor;
            }
        }

        public void Recenter()
        {
            Vector3 c = Center;
            for (int i = 0; i < vertices.Count; i++)
            {
                vertices[i] -= c;
            }
        }

        public void ExtrudeFace(int faceIdx, Vector3 offset)
        {
            bool erase = false;
            Face f = faces[faceIdx];
            for (int i = 0; i < f.edges.Length; i++)
            {
                if (f.edges[i].otherFace != -1)
                {
                    erase = true;
                    break;
                }
            }
            if (!erase)
            {
                // Create a new face with the same data, but every edge connects to the previous face
                Face duplicate = Face.create(f.vertices.Length);
                for (int i = 0; i < f.vertices.Length; i++)
                {
                    duplicate.vertices[i] = f.vertices[f.vertices.Length - i - 1];
                }
                for (int i = 0; i < f.edges.Length; i++)
                {
                    duplicate.edges[i].otherFace = faceIdx;
                }
                faceIdx = faces.Count;
                faces.Add(duplicate);
                f = duplicate;
            }
            // extrude all vertices along offset
            int numVertices = f.vertices.Length;
            int firstVertexIdx = vertices.Count;
            for (int i = 0; i < f.vertices.Length; i++)
            {
                Vector3 extruded = vertices[f.vertices[i]] + offset;
                vertices.Add(extruded);
            }
            // every edge becomes a quad
            int numQuads = f.edges.Length;
            int firstQuadIdx = faces.Count;
            for (int i = 0; i < f.edges.Length; i++)
            {
                Edge e = f.edges[i];
                Face quad = Face.create(4);
                quad.vertices = new int[4];
                quad.vertices[0] = f.vertices[i];
                quad.vertices[1] = f.vertices[(i + 1) % f.vertices.Length];
                quad.vertices[2] = firstVertexIdx + (i + 1) % numVertices;
                quad.vertices[3] = firstVertexIdx + i;
                quad.edges = new Edge[4];
                quad.edges[0] = e;
                quad.edges[1].otherFace = firstQuadIdx + (i + 1) % numQuads;
                quad.edges[2].otherFace = firstQuadIdx + (i + numQuads - 1) % numQuads;
                quad.edges[3].otherFace = faceIdx;
                f.edges[i].otherFace = firstQuadIdx + i;
                faces.Add(quad);
            }
            // now move the face to the new vertices
            for (int i = 0; i < f.vertices.Length; i++)
            {
                f.vertices[i] = firstVertexIdx + i;
            }
        }

        public void SubdivideFace(int face, int times)
        {
        }
    }
    */

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
    private List<GridEdge> edgesCrossingSurface;

    [Range(0.5f, 128)]
    public float gridRadius = 8.0f;

    [Range(4, 128)]
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

    private VolumeGrid netVertices;

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
                        // VERY IMPORTANT: keep this in ascending order!
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
                        Vector3 v0 = VectorFromCoordinate(e.v0);
                        Vector3 v1 = VectorFromCoordinate(e.v1);
                        float t = e.t;
                        Vector3 p = t * v0 + (1 - t) * v1;
                        float d = Distance(p);
                        for (int q = 0; q < 4 && Math.Abs(d) > 0.0001; q++)
                        {
                            t += cornerScale * d;
                            p = t * v0 + (1 - t) * v1;
                        }
                        e.t = t;
                        edgesCrossingSurface.Add(e);
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
        netVertices = new VolumeGrid(gridSize);
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
                        gridEdge.v0 = c0;
                        gridEdge.v1 = c1;
                        int idx = edgesCrossingSurface.BinarySearch(gridEdge);
                        //int idx = edgesCrossingSurface.FindIndex((GridEdge e) => gridEdge.CompareTo(e) == 0);
                        if (idx < 0)
                        {
                            //Debug.Log(String.Format(
                            //"Edge not found {0}->{1} of voxel {2}",
                            //UnpackCubeVertex(iV0), UnpackCubeVertex(iV1), cBase));
                            errors++;
                            errorEdges.Add(gridEdge);
                            continue;
                        }
                        Vector3 v0 = VectorFromCoordinate(c0);
                        Vector3 v1 = VectorFromCoordinate(c1);
                        float t = edgesCrossingSurface[idx].t;
                        sum += t * v0 + (1 - t) * v1;
                        nEdges++;
                    }
                    if (nEdges > 0)
                    {
                        netVertices.Add(cBase, sum / nEdges);
                        //sum = VectorFromCoordinate(cBase);
                        //meshVertices.Add(sum + 0.5f * cornerScale * Vector3.one);
                    }
                }
            }
        }
        Debug.Log("Total errors " + errors);
        currentStep = AlgorithmStep.VerticesConstructed;
    }

    public void AlgorithmCreateMesh()
    {
        MeshFilter mf = GetComponent<MeshFilter>();
        if (mf == null)
        {
            Debug.Log("No mesh filter assigned!");
            return;
        }
        mf.mesh = null;
        Mesh mesh = new Mesh();
        int[] triangles = new int[edgesCrossingSurface.Count * 2 * 3];
        Vector3[] vertices = new Vector3[edgesCrossingSurface.Count * 4];
        int iTriangle = 0;
        int iVertex = 0;
        foreach (GridEdge edge in edgesCrossingSurface)
        {
            GridCoordinate c0 = edge.v0;
            GridCoordinate c1 = edge.v1;
            Debug.Assert(c0.CompareTo(c1) != 0);
            Vector3 vmid = edge.t * VectorFromCoordinate(edge.v0) + (1 - edge.t) * VectorFromCoordinate(edge.v1);
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
            Vector3 v;
            int iStartVertex = iVertex;
            for (int k = cBase.k - dk; k <= cBase.k; k++)
            {
                for (int j = cBase.j - dj; j <= cBase.j; j++)
                {
                    for (int i = cBase.i - di; i <= cBase.i; i++)
                    {
                        bool found = netVertices.Get(out v, new GridCoordinate(i, j, k));
                        Debug.Assert(found);
                        if (!found)
                        {
                            return;
                        }
                        vertices[iVertex++] = v;
                    }
                }
            }
            //vertices[iVertex++] = vmid;
            Debug.Assert(iVertex - iStartVertex == 4);
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
                triangles[iTriangle++] = iStartVertex;
                triangles[iTriangle++] = iStartVertex + winding[i];
                triangles[iTriangle++] = iStartVertex + winding[i + 1];
            }
        }
        if (iTriangle != triangles.Length)
        {
            Debug.Break();
        }
        if (iVertex != vertices.Length)
        {
            Debug.Break();
        }
        Vector3[] normals = new Vector3[vertices.Length];
        for (int i = 0; i < vertices.Length; i++)
        {
            normals[i] = Gradient(vertices[i]);
        }
        mesh.vertices = vertices;
        mesh.normals = normals;
        mesh.triangles = triangles;
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
            Vector3 v0 = VectorFromCoordinate(e.v0) + transform.position;
            Vector3 v1 = VectorFromCoordinate(e.v1) + transform.position;
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
            Vector3 v0 = VectorFromCoordinate(e.v0) + transform.position;
            Vector3 v1 = VectorFromCoordinate(e.v1) + transform.position;
            Gizmos.DrawLine(v0, v1);
        }
    }

    public void DrawVertexGizmos()
    {
        float size = 0.2f;
        Gizmos.color = Color.black;
        foreach (Vector3 v in netVertices)
        {
            Vector3 pos = v + transform.position;
            Gizmos.DrawCube(pos, size * cornerScale * Vector3.one);
        }
    }

    private void Start()
    {
        AlgorithmClear();
        //UpdateMesh();
    }
}