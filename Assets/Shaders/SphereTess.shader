Shader "Surface/SphereTess" {
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
	return length(p) - 1;
}

float _dist_xform_1(float3 p) {
    return _dist_1(qrot(qinv(float4(0,0,0,1)), p - float3(0,0,0)));
}

/////////////////////
// END CODE
/////////////////////
        #define _DIST_FUNCTION _dist_xform_1
        #include "TessMain.cginc"
        ENDCG
    }
    FallBack "Diffuse"
}
