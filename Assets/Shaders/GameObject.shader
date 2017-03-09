Shader "Unlit/GameObject" {
   Properties {
        _Width_1("Width", Float) = 1
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

float _Width_1;

float _dist_1(float3 p) {
	float3 q = abs(p) - float3(_Width_1, _Width_1, _Width_1);
	return max(max(q.x, q.y), q.z);
}

float _Radius_1;

float _dist_2(float3 p) {
	return length(p) - _Radius_1;
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
