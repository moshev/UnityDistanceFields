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
}