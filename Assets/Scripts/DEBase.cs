using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public abstract class DEBase : MonoBehaviour
{
    protected abstract float Distance(Vector3 p);

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

    public Vector3 Gradient(Vector3 centre, float epsilon = 0.0001f)
    {
        Vector3 gradient;
        gradient.x = Distance(centre + epsilon * Vector3.right) - Distance(centre - epsilon * Vector3.right);
        gradient.y = Distance(centre + epsilon * Vector3.up) - Distance(centre - epsilon * Vector3.up);
        gradient.z = Distance(centre + epsilon * Vector3.forward) - Distance(centre - epsilon * Vector3.forward);
        return gradient;
    }

    public void UpdateMesh()
    {
        MeshFilter mf = GetComponent<MeshFilter>();
        if (mf == null)
        {
            Debug.Log("No mesh filter assigned!");
            return;
        }
        Mesh mesh = new Mesh();
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        Vector3[] corners = new Vector3[]
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
        int[] quadCorners = new int[]
        {
            1,4,7,2,
            5,0,3,6,
            3,2,7,6,
            5,4,1,0,
            0,1,2,3,
            4,5,6,7,
        };
        const int subdivs = 48;
        const int gridSize = subdivs + 2;
        const float radius = 8.0f;
        const float cornerScale = 2.0f * radius / subdivs;
        float[,,] distances = new float[gridSize, gridSize, gridSize];
        for (int k = 0; k < gridSize; k++)
        {
            bool kEdge = k == 0 || k == gridSize - 1;
            for (int j = (kEdge ? 1 : 0); j < (kEdge ? gridSize - 1 : gridSize); j++)
            {
                bool jkEdge = kEdge || (j == 0 || j == gridSize - 1);
                for (int i = (jkEdge ? 1 : 0); i < (jkEdge ? gridSize - 1 : gridSize); i++)
                {
                    Vector3 centre;
                    centre.x = (float)(i - 1f) * (2f * radius / subdivs) - radius;
                    centre.y = (float)(j - 1f) * (2f * radius / subdivs) - radius;
                    centre.z = (float)(k - 1f) * (2f * radius / subdivs) - radius;
                    distances[i, j, k] = Distance(centre);
                }
            }
        }
        const int cornersGridSize = subdivs + 1;
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
                    centre.x = (float)i * (2 * radius / subdivs) - radius;
                    centre.y = (float)j * (2 * radius / subdivs) - radius;
                    centre.z = (float)k * (2 * radius / subdivs) - radius;
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
                        if (distances[i + 1 + di, j + 1 + dj, k + 1 + dk] < 0) continue;
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
                                vertices.Add(centre + cornerScale * corners[quadCorners[iCorner + iVertex]]);
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
        // do one round of smoothing
        for (int i = 0; i < vertices.Count; i++)
        {
            Vector3 normal = Gradient(vertices[i]).normalized;
            vertices[i] -= Distance(vertices[i]) * normal;
        }

        Debug.Log(string.Format("Created mesh with {0} vertices and {1} indices ({2} triangles)", vertices.Count, triangles.Count, triangles.Count / 3));

        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals();
        mf.mesh = mesh;
    }

    private void Start()
    {
        UpdateMesh();
    }
}