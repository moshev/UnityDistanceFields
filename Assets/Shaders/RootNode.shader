Shader "Unlit/IntersectOld" {
   Properties {
        _Radius_1("Radius", Float) = 1
        _Radius_2("Radius", Float) = 1
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

float _Radius_2;

float _dist_2(float3 p) {
	return length(p) - _Radius_2;
}

float _dist_3(float3 p) {
	return max(_dist_1(p), _dist_2(p));
}

/////////////////////
// END CODE
/////////////////////
            #define _DIST_FUNCTION _dist_3
            #include "RaymarchMain.cginc"
            ENDCG
        }
    }
    FallBack "Diffuse"
}
