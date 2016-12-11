using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DEMandelbulb : DEBase
{
    [Range(16, 512)]
    public int Iterations;

    [Range(2, 8)]
    public int Power;

    [Range(0, 10)]
    public float Bailout = 2;

    protected override float Distance(Vector3 p)
    {
        Vector3 z = p;
        float dr = 1f;
        float r = 0f;
        for (int i = 0; i < Iterations; i++)
        {
            r = z.magnitude;
            if (r > Bailout) break;

            // convert to polar coordinates
            float theta = (float)Math.Acos(z.z / r);
            float phi = (float)Math.Atan2(z.y, z.x);
            dr = (float)Math.Pow(r, Power - 1.0) * Power * dr + 1.0f;

            // scale and rotate the point
            float zr = (float)Math.Pow(r, Power);
            theta = theta * Power;
            phi = phi * Power;

            // convert back to cartesian coordinates
            z = zr * new Vector3(
                (float)(Math.Sin(theta) * Math.Cos(phi)),
                (float)(Math.Sin(phi) * Math.Sin(theta)),
                (float)(Math.Cos(theta)));
            z += p;
        }
        return (float)(0.5 * Math.Log(r) * r / dr);
    }
}