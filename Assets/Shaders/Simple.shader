Shader "Unlit/Simple" {
   Properties {
        _Width_1("Width", Float) = 1
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
float3 _transform_1;

float _Width_1;

float _dist_1(float3 p) {
	float3 q = abs(p) - float3(_Width_1, _Width_1, _Width_1);
	return max(max(q.x, q.y), q.z);
}

float _dist_xform_1(float3 p) {
    return _dist_1(p - _transform_1);
}

/////////////////////
// END CODE
/////////////////////
            #define _DIST_FUNCTION _dist_xform_1
            #include "RaymarchMain.cginc"
            ENDCG
        }
    }
    FallBack "Diffuse"
}
