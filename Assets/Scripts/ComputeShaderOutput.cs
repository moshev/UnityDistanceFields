using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ComputeShaderOutput : MonoBehaviour
{
    /// <summary>
    ///  Compute shader to use
    /// </summary>
    public ComputeShader computeShader;

    /// <summary>
    /// Total number of vertices to calculate
    /// </summary>
    public const int VertCount = 10 * 10 * 10 * 10 * 10 * 10;

    /// <summary>
    /// Storage for result from the Compute Shader.
    /// </summary>
    public ComputeBuffer outputBuffer;

    public Shader PointShader;
    private Material PointMaterial;

    public bool DebugRender = false;

    private int CSKernel;

    private void InitializeBuffers()
    {
        outputBuffer = new ComputeBuffer(VertCount, (sizeof(float) * 3 + sizeof(int) * 6));
        computeShader.SetBuffer(CSKernel, "outputBuffer", outputBuffer);
        if (DebugRender)
        {
            PointMaterial.SetBuffer("buf_Points", outputBuffer);
        }
    }

    public void Dispatch()
    {
        if (!SystemInfo.supportsComputeShaders)
        {
            Debug.LogWarning("Compute shaders not supported!");
            return;
        }
        computeShader.Dispatch(CSKernel, 10, 10, 10);
    }

    private void ReleaseBuffers()
    {
        outputBuffer.Release();
    }

    private void Start()
    {
        CSKernel = computeShader.FindKernel("CSMain");

        if (DebugRender)
        {
            PointMaterial = new Material(PointShader);
            PointMaterial.SetVector("_worldPos", transform.position);
        }

        InitializeBuffers();
    }

    private void OnRenderObject()
    {
        if (DebugRender)
        {
            Dispatch();
            PointMaterial.SetPass(0);
            PointMaterial.SetVector("_worldPos", transform.position);

            Graphics.DrawProcedural(MeshTopology.Points, VertCount);
        }
    }

    private void OnDisable()
    {
        ReleaseBuffers();
    }
}