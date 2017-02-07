Shader "Unlit / Sphere" {
   Properties {
        _Radius_1("Radius", Float) = 1
        _CanvasSize("CanvasSize", Float) = 1
    }
    SubShader {
        Tags { "RenderType" = "Opaque" }
        LOD 200
        Pass {
            Cull Back
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
/////////////////////
// BEGIN CODE
/////////////////////

float _Radius_1;

float _dist_1(float3 p) {
	return length(p) - _Radius_1;
}

/////////////////////
// END CODE
/////////////////////
            #define _DIST_FUNCTION _dist_1
            #include "RaymarchMain.cginc"
            ENDCG
        }
    }
    FallBack "Diffuse"
}
