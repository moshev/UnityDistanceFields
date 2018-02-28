Shader "Surface/Fig3Tess" {
   Properties {
        _EdgeLength ("Tessellation edge Length", Range(2,50)) = 15
        _MaxDisplacement ("Maximum object-space displacement", Range(0,1)) = 0.1
        _MainTex ("Main texture", 2D) = "white" {}
        _Color ("Color", color) = (1,1,1,0)
        _Specular ("Specular", Range(0,1)) = 0.5
    }
    SubShader {
        Tags { "RenderType" = "Opaque" }
        LOD 200
        CGPROGRAM
        #pragma surface surf Standard addshadow fullforwardshadows vertex:disp tessellate:tess nolightmap
        #pragma target 4.6
        #include "Tessellation.cginc"
        #include "RaymarchUtils.cginc"
/////////////////////
// BEGIN CODE
/////////////////////





float _dist_1(float3 p) {
    //float x = max(p.x - float3(2.7*0.5, 0, 0),-p.x - float3(2.7*0.5, 0, 0));
    //float y = max(p.y - float3(0.22*0.5, 0, 0),-p.y - float3(0.22*0.5, 0, 0));
    //float z = max(p.z - float3(2.49*0.5, 0, 0),-p.z - float3(2.49*0.5, 0, 0));
    float x = max(p.x - 2.7*0.5, -p.x - 2.7*0.5);
    float y = max(p.y - 0.22*0.5, -p.y - 0.22*0.5);
    float z = max(p.z - 2.49*0.5, -p.z - 2.49*0.5);
    float d = x;
    d = max(d,y);
    d = max(d,z);
    return d;
}

float _dist_xform_1(float3 p) {
    return _dist_1(qrot(qinv(float4(0,0,0,1)), p - float3(0,0,0)));
}




float _dist_2(float3 p) {
    return lerp(length(p.xz) - 0.09, length(float3(p.x, abs(p.y) - 1.11, p.z)) - 0.09,
            step(1.11, abs(p.y)));
}

float _dist_xform_2(float3 p) {
    return _dist_2(qrot(qinv(float4(0,0,0,1)), p - float3(0.494,0.591,0.671)));
}

float _dist_3(float3 p) {
    float a = _dist_xform_1(p);
    float b = _dist_xform_2(p);
    return max(a, -b);
}

float _dist_xform_3(float3 p) {
    return _dist_3(p);
}

/////////////////////
// END CODE
/////////////////////
        #define _DIST_FUNCTION _dist_xform_3
        #include "TessMain.cginc"
        ENDCG
    }
    FallBack "Diffuse"
}
