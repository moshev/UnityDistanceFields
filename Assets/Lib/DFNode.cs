﻿using System.Collections.Generic;
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
    public string translationUniform;
	public string quaternionUniform;

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
        translationUniform = nm.makeUnique("_translation");
		quaternionUniform = nm.makeUnique("_rotation");
        body.Append(string.Format("float3 {0};", translationUniform));
        body.Append(System.Environment.NewLine);
		body.Append(string.Format("float4 {0};", quaternionUniform));
        body.Append(System.Environment.NewLine);
        string distFunction = nm.makeUnique("_dist_xform");
        string distSub = nm.makeUnique("_dist");
        mangledFragment.Replace("float _dist(", "float " + distSub + "(");
        body.Append(mangledFragment);
        body.Append(string.Format(@"
float {0}(float3 p) {{
    return {1}(qrot(qinv({2}), p - {3}));
}}
", distFunction, distSub, quaternionUniform, translationUniform));
        return distFunction;
    }

    public void SetTransformsInMaterial(Material mat, bool skipThis)
    {
        if (children.Count == 0)
        {
			Vector3 vec;
			Quaternion q;
			if (skipThis)
			{
				vec = Vector3.zero;
				q = Quaternion.identity;
			}
			else
			{
				vec = transform.position;
				q = transform.rotation;
			}
			Vector4 qv = new Vector4(q.x, q.y, q.z, q.w);
			mat.SetVector(translationUniform, vec);
			mat.SetVector(quaternionUniform, qv);
        }
        foreach (DFNodeChild child in children)
        {
            child.node.SetTransformsInMaterial(mat, false);
        }
    }

    public void SetTransformsInComputeShader(ComputeShader shader, bool skipThis)
    {
        if (children.Count == 0)
        {
			Vector3 vec;
			Quaternion q;
			if (skipThis)
			{
				vec = Vector3.zero;
				q = Quaternion.identity;
			}
			else
			{
				vec = transform.position;
				q = transform.rotation;
			}
			Vector4 qv = new Vector4(q.x, q.y, q.z, q.w);
			shader.SetVector(translationUniform, vec);
			shader.SetVector(quaternionUniform, qv);
        }
        foreach (DFNodeChild child in children)
        {
            child.node.SetTransformsInComputeShader(shader, false);
        }
    }

    public void CreateShaderAsset(string assetPath)
    {
        GlobalNameManager nm = new GlobalNameManager();
        List<DFNodeProperty> properties = new List<DFNodeProperty>();
        StringBuilder bodyBuilder = new StringBuilder();
        string distFunction = GetFragments(nm, properties, bodyBuilder);
        string shaderName = Path.GetFileNameWithoutExtension(assetPath);
        using (StreamWriter fout = new StreamWriter(assetPath))
        {
            fout.WriteLine("Shader \"Unlit/" + shaderName + "\" {");
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
            fout.WriteLine("        Tags { \"RenderType\" = \"Opaque\" \"Queue\" = \"Transparent\" }");
            fout.WriteLine("        LOD 200");
            fout.WriteLine("        Pass {");
            fout.WriteLine("            Cull Back");
            fout.WriteLine("            CGPROGRAM");
            fout.WriteLine("            #pragma vertex vert");
            fout.WriteLine("            #pragma fragment frag");
            fout.WriteLine("            #include \"UnityCG.cginc\"");
            fout.WriteLine("            #include \"RaymarchUtils.cginc\"");
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

    public void CreateComputeAsset(string assetPath)
    {
        GlobalNameManager nm = new GlobalNameManager();
        List<DFNodeProperty> properties = new List<DFNodeProperty>();
        StringBuilder bodyBuilder = new StringBuilder();
        string distFunction = GetFragments(nm, properties, bodyBuilder);
        using (StreamWriter fout = new StreamWriter(assetPath))
        {
            fout.WriteLine("#pragma kernel RaymarchMain");
            fout.WriteLine("#pragma kernel DistanceMain");
            fout.WriteLine("#define _DIST_FUNCTION " + distFunction + "");
            fout.WriteLine("float " + distFunction + "(float3 p);");
            fout.WriteLine("#include \"RaymarchUtils.cginc\"");
            fout.WriteLine("#include \"RaymarchMainCompute.cginc\"");
            fout.WriteLine("StructuredBuffer<raycontext> _input;");
            fout.WriteLine("RWStructuredBuffer<rayresult> _output;");
            fout.WriteLine("[numthreads(128, 1, 1)]");
            fout.WriteLine("void RaymarchMain(uint3 dispatchId : SV_DispatchThreadID)");
            fout.WriteLine("{");
            fout.WriteLine("    uint arrId = dispatchId.x;");
            fout.WriteLine("    rayresult res;");
            fout.WriteLine("    res = trace(_input[arrId]);");
            fout.WriteLine("    _output[arrId] = res;");
            fout.WriteLine("}");
            fout.WriteLine("[numthreads(128, 1, 1)]");
            fout.WriteLine("void DistanceMain(uint3 dispatchId : SV_DispatchThreadID)");
            fout.WriteLine("{");
            fout.WriteLine("    uint arrId = dispatchId.x;");
            fout.WriteLine("    rayresult res;");
            fout.WriteLine("    res.p = float3(0, 0, 0);");
            fout.WriteLine("    res.n = float3(0, 0, 0);");
            fout.WriteLine("    res.distance = _DIST_FUNCTION(_input[arrId].p);");
            fout.WriteLine("    _output[arrId] = res;");
            fout.WriteLine("}");
            fout.Write(bodyBuilder);
            fout.WriteLine();
        }
    }
}