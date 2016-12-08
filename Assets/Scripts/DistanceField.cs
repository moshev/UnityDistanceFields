using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class DistanceField : MonoBehaviour
{
    public DEBase distanceEstimator;
    public float radius;

    // Use this for initialization
    private void Start()
    {
        MeshFilter mf = GetComponent<MeshFilter>();
        if (mf == null)
        {
            Debug.Log("No mesh filter assigned!");
            return;
        }
        Mesh mesh = new Mesh();
        Vector3[] vertices = new Vector3[4] {
            new Vector3(-radius, -radius, 0),
            new Vector3( radius, -radius, 0),
            new Vector3( radius,  radius, 0),
            new Vector3(-radius,  radius, 0)
        };
        int[] indices = new int[6]
        {
            0, 1, 2,
            0, 2, 3
        };
        mesh.vertices = vertices;
        mesh.triangles = indices;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        mf.mesh = mesh;
    }

    // Update is called once per frame
    private void Update()
    {
        //transform.LookAt(Camera.main.transform.position);
    }
}