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
        DrawDefaultInspector();
        if (GUILayout.Button("Recreate mesh"))
        {
            de.UpdateMesh();
        }
    }
}