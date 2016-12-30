﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class DFRenderer : MonoBehaviour
{
    public Material material;

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

    private Renderer renderer;

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
        renderer = GetComponent<Renderer>();
        renderer.material = material;
    }

    private void OnEnable()
    {
        if (renderer)
        {
            renderer.material = material;
        }
    }

    // Update is called once per frame
    private void Update()
    {
    }
}