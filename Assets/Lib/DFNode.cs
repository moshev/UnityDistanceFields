using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.IO;
using UnityEngine;

[System.Serializable]
public struct DFNodeProperty
{
    public static string CHILD_NODE = "(ChildNode)";
    public string name;
    public string fragment;
}

[System.Serializable]
public class DFNodeChild
{
    public string name;
    public DFNode node;

    public DFNodeChild(string name, DFNode node)
    {
        this.name = name;
        this.node = node;
    }
}

[ExecuteInEditMode]
public class DFNode : MonoBehaviour
{
    public string prevFragmentPath = "";
    public string fragmentPath = "";
    public string nodeName;
    public List<DFNodeChild> children = new List<DFNodeChild>();
    public string bodyFragment;
    public List<DFNodeProperty> properties;
    public string transformUniform;

    // Use this for initialization
    private void Start()
    {
    }

    // Update is called once per frame
    private void Update()
    {
    }

    private string GetFragments(GlobalNameManager nm, List<DFNodeProperty> outProperties, StringBuilder body)
    {
        StringBuilder mangledFragment = new StringBuilder(bodyFragment);
        foreach (DFNodeChild child in children)
        {
            string functionName = child.node.GetFragments(nm, outProperties, body);
            Debug.Log("Replace child " + child.name + " with " + functionName);
            mangledFragment.Replace(child.name, functionName);
        }
        foreach (DFNodeProperty property in properties)
        {
            DFNodeProperty mangled = new DFNodeProperty();
            mangled.name = nm.makeUnique(property.name);
            mangled.fragment = property.fragment;
            outProperties.Add(mangled);
            mangledFragment.Replace(property.name, mangled.name);
        }
        transformUniform = nm.makeUnique("_transform");
        body.Append(string.Format("float3 {0};\n", transformUniform));
        string distFunction = nm.makeUnique("_dist_xform");
        string distSub = nm.makeUnique("_dist");
        mangledFragment.Replace("float _dist(", "float " + distSub + "(");
        body.Append(mangledFragment);
        body.Append(string.Format(@"
float {0}(float3 p) {{
    return {1}(p - {2});
}}
", distFunction, distSub, transformUniform));
        return distFunction;
    }

    public void SetTransformsInMaterial(Material mat)
    {
        mat.SetVector(transformUniform, transform.position);
        foreach (DFNodeChild child in children)
        {
            child.node.SetTransformsInMaterial(mat);
        }
    }

    public void CreateShaderAsset(string assetPath)
    {
        GlobalNameManager nm = new GlobalNameManager();
        List<DFNodeProperty> properties = new List<DFNodeProperty>();
        StringBuilder bodyBuilder = new StringBuilder();
        string distFunction = GetFragments(nm, properties, bodyBuilder);
        using (StreamWriter fout = new StreamWriter(assetPath))
        {
            fout.WriteLine("Shader \"Unlit/" + nodeName + "\" {");
            fout.WriteLine("   Properties {");
            foreach (DFNodeProperty property in properties)
            {
                fout.Write("        ");
                fout.Write(property.name);
                fout.Write(property.fragment);
                fout.WriteLine();
            }
            fout.WriteLine("        _CanvasSize(\"CanvasSize\", Float) = 1");
            fout.WriteLine("    }");
            fout.WriteLine("    SubShader {");
            fout.WriteLine("        Tags { \"RenderType\" = \"Opaque\" }");
            fout.WriteLine("        LOD 200");
            fout.WriteLine("        Pass {");
            fout.WriteLine("            Cull Back");
            fout.WriteLine("            CGPROGRAM");
            fout.WriteLine("            #pragma vertex vert");
            fout.WriteLine("            #pragma fragment frag");
            fout.WriteLine("            #include \"UnityCG.cginc\"");
            fout.WriteLine("/////////////////////");
            fout.WriteLine("// BEGIN CODE");
            fout.WriteLine("/////////////////////");
            fout.Write(bodyBuilder);
            fout.WriteLine();
            fout.WriteLine("/////////////////////");
            fout.WriteLine("// END CODE");
            fout.WriteLine("/////////////////////");
            fout.WriteLine("            #define _DIST_FUNCTION " + distFunction + "");
            fout.WriteLine("            #include \"RaymarchMain.cginc\"");
            fout.WriteLine("            ENDCG");
            fout.WriteLine("        }");
            fout.WriteLine("    }");
            fout.WriteLine("    FallBack \"Diffuse\"");
            fout.WriteLine("}");
        }
    }
}