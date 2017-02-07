using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(DFNode))]
public class DFNodeEditor : Editor
{
    public void OnEnable()
    {
    }

    public override void OnInspectorGUI()
    {
        DFNode node = (DFNode)target;
        string path = node.fragmentPath;
        if (string.IsNullOrEmpty(path))
        {
            path = "Please select a file";
        }
        EditorGUILayout.LabelField(path);
        EditorGUILayout.LabelField(node.nodeName);
        if (GUILayout.Button("Browse"))
        {
            string newpath = EditorUtility.OpenFilePanelWithFilters("Select distance field fragment", "Assets/Fragments", new string[] { "Distance field fragments", "dfinc" });
            if (!string.IsNullOrEmpty(newpath))
            {
                node.fragmentPath = newpath;
            }
        }
        if (GUILayout.Button("Parse"))
        {
            try
            {
                ParseFragment(node.fragmentPath, node);
            }
            catch (System.Exception e)
            {
                EditorUtility.DisplayDialog("Parse error", e.Message, "OK");
            }
        }
        if (GUILayout.Button("Generate"))
        {
            string newpath = EditorUtility.SaveFilePanel("Save as...", "Assets/Shaders", node.name + ".shader", "shader");
            node.CreateShaderAsset(newpath);
        }
    }

    public void ParseFragment(string path, DFNode node)
    {
        int offset = 0;
        string text = File.ReadAllText(path);
        RegexOptions opts = RegexOptions.Multiline | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase;
        Match matchName = new Regex(@"^// name = (.*)", opts).Match(text, offset);
        offset = matchName.Index + matchName.Length;
        if (!matchName.Success)
        {
            throw new System.Exception("Couldn't find name part");
        }
        Match propertiesMatch = new Regex(@"^// properties\s*$", opts).Match(text, offset);
        if (!propertiesMatch.Success)
        {
            throw new System.Exception("Couldn't find properties part");
        }
        offset = propertiesMatch.Index + propertiesMatch.Length;
        Match cgprogramMatch = new Regex(@"^// cgprogram\s*$", opts).Match(text, offset);
        if (!cgprogramMatch.Success)
        {
            throw new System.Exception("Couldn't find cgprogram part");
        }
        Regex propertyRegex = new Regex(@"^\s*(_\w*)([(].*)", opts);
        List<DFNodeProperty> properties = new List<DFNodeProperty>();
        for (offset = propertiesMatch.Index + propertiesMatch.Length; offset < cgprogramMatch.Index;)
        {
            Match match = propertyRegex.Match(text, offset);
            if (!match.Success)
            {
                break;
            }
            DFNodeProperty property = new DFNodeProperty();
            property.name = match.Groups[1].Value;
            property.fragment = match.Groups[2].Value.Trim();
            properties.Add(property);
            offset = match.Index + match.Length;
        }
        node.nodeName = matchName.Groups[1].Value.Trim();
        node.properties = properties.ToArray();
        node.bodyFragment = text.Substring(cgprogramMatch.Index + cgprogramMatch.Length);
    }
}