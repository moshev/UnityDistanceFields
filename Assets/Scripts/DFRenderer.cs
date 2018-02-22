using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;

[ExecuteInEditMode]
public class DFRenderer : MonoBehaviour
{
    public bool fullScene = false;

    private static Vector3[] gVertices = new Vector3[]
    {
        new Vector3(-1, -1),
        new Vector3(-1, +1),
        new Vector3(+1, +1),
        new Vector3(+1, -1),
    };

    private static Vector3[] gNormals = new Vector3[]
    {
        Vector3.forward,
        Vector3.forward,
        Vector3.forward,
        Vector3.forward,
    };

    private static int[] gTriangles = new int[]
    {
        0, 1, 2,
        0, 2, 3,
    };

    // Use this for initialization
    private void Start()
    {
        Mesh mesh = new Mesh();
        mesh.vertices = gVertices;
        mesh.triangles = gTriangles;
        mesh.normals = gNormals;
        if (!GetComponent<MeshFilter>() || !GetComponent<MeshRenderer>())
        {
            gameObject.AddComponent<MeshFilter>();
            gameObject.AddComponent<MeshRenderer>();
        }
        GetComponent<MeshFilter>().mesh = mesh;
    }

    private void OnEnable()
    {
    }

    // Update is called once per frame
    private void Update()
    {
        DFNode df = GetComponent<DFNode>();
        if (!df) return;
        df.SetTransformsInMaterial(GetComponent<Renderer>().sharedMaterial, true);
        if (fullScene)
        {
            Transform t = transform;
            Transform c;
            Camera cur = Camera.main;
            if (cur != null)
            {
                c = cur.transform;
            }
            else
            {
                SceneView v = SceneView.lastActiveSceneView;
                cur = v.camera;
                c = cur.transform;
            }
            t.position = c.TransformVector(Vector3.forward);
        }
    }

    public void UpdateMaterial()
    {
        AssetDatabase.Refresh(ImportAssetOptions.Default);
    }

    private void OnGUI()
    {
    }
}