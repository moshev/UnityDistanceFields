using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(DFRenderer))]
public class DFRendererEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        DFRenderer renderer = (DFRenderer)target;
        if (GUILayout.Button("Update material"))
        {
            renderer.UpdateMaterial();
        }
    }
}