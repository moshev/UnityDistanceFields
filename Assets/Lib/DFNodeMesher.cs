using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEditor;

public class DFNodeMesher
{
    public ComputeShader distanceEstimator;
    private DFNode _dn;

    public DFNode dfNode
    {
        get
        {
            return _dn;
        }
        set
        {
            // TODO: magic
        }
    }

    public DFNodeMesher()
    {
    }

    public void AlgorithmClear()
    {
    }

    public void AlgorithmCalculateDistances(ProgressReport prog)
    {
    }

    public void AlgorithmFindEdgeIntersections(ProgressReport prog)
    {
    }

    public void AlgorithmConstructVertices(ProgressReport prog)
    {
    }

    public void AlgorithmCreateMesh(ProgressReport prog)
    {
    }
}