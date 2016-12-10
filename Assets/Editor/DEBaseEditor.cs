using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(DEBase), true)]
public class DEBaseEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DEBase de = target as DEBase;
        GUILayout.Label("Current step: " + de.CurrentStep.ToString());
        DrawDefaultInspector();
        /*
        if (GUILayout.Button("Recreate mesh"))
        {
            de.UpdateMesh();
        }
        */
        if (GUILayout.Button("Step0 - clear"))
        {
            de.AlgorithmClear();
        }
        if (GUILayout.Button("Step1 - calculate distances"))
        {
            de.AlgorithmCalculateDistances();
        }
        if (GUILayout.Button("Step 2 - find intersecting edges"))
        {
            de.AlgorithmFindEdgeIntersections();
        }
        if (GUILayout.Button("Step 3 - calculate net vertices"))
        {
            de.AlgorithmConstructVertices();
        }
        if (GUILayout.Button("Step 4 - make mesh"))
        {
            de.AlgorithmCreateMesh();
        }
    }
}