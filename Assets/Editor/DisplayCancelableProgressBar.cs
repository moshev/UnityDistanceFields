using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// Simple Editor Script that fills a cancelable bar in the given seconds.
public class DisplayCancelableProgressBar : EditorWindow
{
    public int secs = 10;
    public double startVal = 0;
    public double progress = 0;

    [MenuItem("Examples/Cancelable Progress Bar Usage")]
    private static void Init()
    {
        UnityEditor.EditorWindow window = GetWindow<DisplayCancelableProgressBar>();
        window.Show();
    }

    private void OnGUI()
    {
        secs = EditorGUILayout.IntField("Time to wait:", secs);
        if (GUILayout.Button("Display bar"))
        {
            if (secs < 1)
            {
                Debug.LogError("Seconds should be bigger than 1");
                return;
            }
            startVal = EditorApplication.timeSinceStartup;
        }
        if (progress < secs)
        {
            if (EditorUtility.DisplayCancelableProgressBar(
                "Simple Progress Bar",
                "Shows a progress bar for the given seconds",
                (float)(progress / secs)))
            {
                Debug.Log("Progress bar canceled by the user");
                startVal = 0;
            }
        }
        else
        {
            EditorUtility.ClearProgressBar();
        }
        progress = EditorApplication.timeSinceStartup - startVal;
    }

    private void OnInspectorUpdate()
    {
        Repaint();
    }
}