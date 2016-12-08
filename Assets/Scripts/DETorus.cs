using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DETorus : DEBase
{
    public Vector3 center = Vector3.zero;
    public Vector3 normal = Vector3.up;
    public float radius1 = 1f;
    public float radius2 = 0.3f;

    protected override float Distance(Vector3 p)
    {
        // equation is
        // (rmax - sqrt(dot(p.xy))) ** 2 + z**2 - rmin**2
        // for torus symmetric around z
        float z = Vector3.Dot(p, normal) - Vector3.Dot(center, normal);
        Vector3 p1 = p - z * normal;
        float xy2 = (p1 - center).sqrMagnitude;
        float b = radius1 - Mathf.Sqrt(xy2);
        return Mathf.Sqrt(b * b + z * z) - radius2;
    }
}