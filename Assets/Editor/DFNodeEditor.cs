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
            try
            {
                ParseFragment(node.fragmentPath, node);
            }
            catch (System.Exception e)
            {
                EditorUtility.DisplayDialog("Parse error", e.Message, "OK");
            }
        }
        if (GUILayout.Button("Re-parse"))
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
        for (int i = 0; i < node.children.Count; i++)
        {
            node.children[i].node = (DFNode)EditorGUILayout.ObjectField(
                node.children[i].name, node.children[i].node, typeof(DFNode), true);
        }
        if (GUILayout.Button("Generate Pixel Shader"))
        {
            string newpath = EditorUtility.SaveFilePanel("Save as...", "Assets/Shaders", node.name + ".shader", "shader");
            node.CreateShaderAsset(newpath);
        }
        if (GUILayout.Button("Generate Tesselation Shader"))
        {
            string newpath = EditorUtility.SaveFilePanel("Save as...", "Assets/Shaders", node.name + "Tess.shader", "shader");
            node.CreateTessellationShader(newpath, node.GetComponent<MeshRenderer>().sharedMaterial);
        }
        if (GUILayout.Button("Generate Compute Shader"))
        {
            string newpath = EditorUtility.SaveFilePanel("Save as...", "Assets/Shaders", node.name + ".compute", "compute");
            node.CreateComputeAsset(newpath);
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
        List<string> childNames = new List<string>();
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
            if (property.fragment == DFNodeProperty.CHILD_NODE)
            {
                childNames.Add(property.name);
            }
            else
            {
                properties.Add(property);
            }
            offset = match.Index + match.Length;
        }
        node.nodeName = matchName.Groups[1].Value.Trim();
        node.properties = new List<DFNodeProperty>(properties);
        node.bodyFragment = text.Substring(cgprogramMatch.Index + cgprogramMatch.Length);
        bool recreateChildren = false;
        if (node.prevFragmentPath == path && node.children != null && node.children.Count == childNames.Count)
        {
            for (int i = 0; i < node.children.Count && !recreateChildren; i++)
            {
                if (node.children[i].name != childNames[i])
                {
                    recreateChildren = true;
                }
            }
        }
        else
        {
            recreateChildren = true;
        }
        if (recreateChildren)
        {
            node.children = new List<DFNodeChild>(childNames.Count);
            foreach (string name in childNames)
            {
                node.children.Add(new DFNodeChild(name, null));
            }
        }
        else
        {
            foreach (DFNodeChild child in node.children)
            {
                if (!child.node) continue;
                ParseFragment(child.node.fragmentPath, child.node);
            }
        }
        node.prevFragmentPath = path;
    }
}