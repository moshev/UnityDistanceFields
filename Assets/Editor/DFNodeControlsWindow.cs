﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class DFNodeControlsWindow : EditorWindow
{
    private DFNodeMesher mesher;
    private ProgressReport operationProgress = new ProgressReport();

    [MenuItem("Custom/Distance Field Node Controls")]
    public static void Init()
    {
        UnityEditor.EditorWindow window = GetWindow<DFNodeControlsWindow>();
        window.Show();
    }

    public void Awake()
    {
        OnSelectionChange();
    }

    private void OnSelectionChange()
    {
        GameObject go = Selection.activeGameObject;
        if (go == null) return;
        DFNode dn = go.GetComponent<DFNode>();
        if (dn == null)
        {
            mesher = null;
        }
        else if (mesher == null)
        {
            mesher = new DFNodeMesher();
            mesher.dfNode = dn;
        }
        else if (mesher.dfNode != dn)
        {
            mesher.dfNode = dn;
        }
    }

    private void OnGUI()
    {
        bool wasEnabled = GUI.enabled;
        ProgressReport.State progressState = operationProgress.CurrentState;
        if (mesher == null)
        {
            GUI.enabled = false;
            GUILayout.Label("Please select an object with a DEBase component");
        }
        else
        {
            GUILayout.Label("Choose algorithm step");
        }
        if (progressState.runStatus != ProgressReport.STATE_NOT_STARTED)
        {
            GUI.enabled = false;
        }
        if (GUILayout.Button("Step0 - clear"))
        {
            mesher.AlgorithmClear();
        }
        if (GUILayout.Button("Step1 - calculate distances"))
        {
            mesher.AlgorithmCalculateDistances(operationProgress);
        }
        if (GUILayout.Button("Step 2 - find intersecting edges"))
        {
            mesher.AlgorithmFindEdgeIntersections(operationProgress);
        }
        if (GUILayout.Button("Step 3 - calculate net vertices"))
        {
            mesher.AlgorithmConstructVertices(operationProgress);
        }
        if (GUILayout.Button("Step 4 - make mesh"))
        {
            mesher.AlgorithmCreateMesh(operationProgress);
        }
        GUI.enabled = wasEnabled;
        string label;
        string message;
        if (progressState.runStatus != ProgressReport.STATE_NOT_STARTED)
        {
            switch (progressState.runStatus)
            {
                case ProgressReport.STATE_RUNNING:
                    label = "Please wait..."; break;
                case ProgressReport.STATE_CANCELLED:
                    label = "Cancelling..."; break;
                default:
                    label = "Unknown state..."; break;
            }
            message = progressState.message;
        }
        else
        {
            label = "Not running";
            message = "";
        }
        if (progressState.runStatus == ProgressReport.STATE_FINISHED)
        {
            try
            {
                operationProgress.RunMainThreadCallback();
                operationProgress.CurrentState = new ProgressReport.State("", 0);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
        EditorGUILayout.LabelField(label);
        Rect r = EditorGUILayout.GetControlRect(false);
        EditorGUI.ProgressBar(r, (float)progressState.progress, message);
    }

    private void Update()
    {
        if (operationProgress.Changed)
        {
            Repaint();
        }
    }
}