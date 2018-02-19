using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class DFNodeControlsWindow : EditorWindow
{
    private ProgressReport operationProgress = new ProgressReport();
    private DFNodeMesher mesher = new DFNodeMesher();
    public ComputeShader shader;
    public DFRenderer renderer;

    [MenuItem("Custom/Distance Field Compute Shader Mesher")]
    public static void Init()
    {
        UnityEditor.EditorWindow window = GetWindow<DFNodeControlsWindow>();
        window.Show();
    }

    private void OnEnable()
    {
        mesher.InitBuffers();
    }

    private void OnGUI()
    {
        bool wasEnabled = GUI.enabled;
        bool error = false;
        ProgressReport.State progressState = operationProgress.CurrentState;
        ComputeShader selShader = (ComputeShader)EditorGUILayout.ObjectField("Compute shader", shader, typeof(ComputeShader), true);
        DFRenderer selRenderer = (DFRenderer)EditorGUILayout.ObjectField("DFRenderer", renderer, typeof(DFRenderer), true);
        MeshRenderer meshRenderer = selRenderer ? selRenderer.gameObject.GetComponent<MeshRenderer>() : null;
        if (selRenderer != null && meshRenderer == null)
        {
            error = true;
            GUILayout.Label("Selected renderer doesn't have a MeshRenderer component!");
        }
        Material selMaterial = null;
        try
        {
            selMaterial = selRenderer.gameObject.GetComponent<MeshRenderer>().sharedMaterial;
        }
        catch (NullReferenceException ignore)
        {
            // ignore
        }
        if (selShader != shader || mesher.distanceEstimator != shader || selRenderer != renderer || mesher.material != selMaterial)
        {
            try
            {
                Debug.Log("Resetting compute shader");
                shader = selShader;
                renderer = selRenderer;
                mesher.distanceEstimator = shader;
                mesher.material = selMaterial;
                mesher.rootNode = selRenderer.gameObject.GetComponent<DFNode>();
                mesher.AlgorithmClear();
                mesher.InitKernel();
            }
            catch (MissingReferenceException e)
            {
                error = true;
            }
            catch (NullReferenceException e)
            {
                error = true;
            }
            operationProgress.CancelProgress();
        }
        if (shader == null || renderer == null)
        {
            error = true;
            GUILayout.Label("Select a Compute Shader and corresponding DFRenderer");
        }
        mesher.gridSize = EditorGUILayout.IntField("Grid subdivisions", mesher.gridSize);
        mesher.gridRadius = EditorGUILayout.FloatField("Grid radius", mesher.gridRadius);
        GUILayout.Label("Choose algorithm step");
        if (error || progressState.runStatus != ProgressReport.STATE_NOT_STARTED)
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
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Step 4 - make mesh"))
        {
            GameObject go = Selection.activeGameObject;
            MeshFilter mf = null;
            if (go != null)
            {
                mf = go.GetComponent<MeshFilter>();
            }
            if (mf != null)
            {
                mesher.AlgorithmCreateMesh(operationProgress, mf);
            }
            else
            {
                Debug.Log("Please select a game object with a mesh filter");
            }
        }
        if (GUILayout.Button("Step 4 - write mesh"))
        {
            string objfile = EditorUtility.SaveFilePanelInProject("Select OBJ file", renderer.gameObject.name + ".obj", "obj", "Enter a file name to save the mesh as", "Models");
            if (!string.IsNullOrEmpty(objfile))
            {
                mesher.AlgorithmWriteMesh(operationProgress, objfile);
            }
        }
        GUILayout.EndHorizontal();
        GUI.enabled = wasEnabled;
        string label;
        string message;
        while (true)
        {
            try
            {
                operationProgress.RunQueuedTasks();
                break;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
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
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            finally
            {
                operationProgress.CurrentState = new ProgressReport.State("", 0);
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