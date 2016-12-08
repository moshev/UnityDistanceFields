using System;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class DESphere : DEBase
{
    public float radius;
    public Vector3 center;

    protected override float Distance(Vector3 p)
    {
        return (p - center).magnitude - radius;
    }

    /*
    protected override Mesh GetBaseMesh()
    {
        ConstructibleMesh construct = ConstructibleMesh.CreateQuad();
        construct.ExtrudeFace(0, -2 * Vector3.forward);
        construct.Recenter();
        construct.Scale(0.5f);
        return construct.ToMesh();
    }
    */
}