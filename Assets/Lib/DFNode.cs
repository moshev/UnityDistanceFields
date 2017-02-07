using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.IO;
using UnityEngine;

public struct DFNodeProperty
{
    public string name;
    public string fragment;
}

[ExecuteInEditMode]
public class DFNode : MonoBehaviour
{
    public string fragmentPath = "";
    public string nodeName;
    public DFNode[] children;
    public string bodyFragment;
    public DFNodeProperty[] properties;

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
        foreach (DFNode child in children)
        {
            child.GetFragments(nm, outProperties, body);
        }
        StringBuilder mangledFragment = new StringBuilder(bodyFragment);
        foreach (DFNodeProperty property in properties)
        {
            DFNodeProperty mangled = new DFNodeProperty();
            mangled.name = nm.makeUnique(property.name);
            mangled.fragment = property.fragment;
            outProperties.Add(mangled);
            mangledFragment.Replace(property.name, mangled.name);
        }
        string distFunction = nm.makeUnique("_dist");
        mangledFragment.Replace("_dist", distFunction);
        body.Append(mangledFragment);
        return distFunction;
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