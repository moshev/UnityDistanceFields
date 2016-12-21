using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(DEBase), true)]
public class DEBaseEditor : Editor
{
    public void Awake()
    {
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        DEBase de = target as DEBase;
        GUILayout.Label("Current step: " + de.CurrentStep.ToString());
        DrawDefaultInspector();
        serializedObject.ApplyModifiedProperties();
    }
}