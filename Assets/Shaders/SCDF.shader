Shader "Unlit/SCDF" {
   Properties {
        _Width_1("Width", Float) = 1
        _Radius_1("Radius", Float) = 1
        _Factor_1("Mix Factor", Float) = 0.5
        _CanvasSize("CanvasSize", Float) = 1
    }
    SubShader {
        Tags { "RenderType" = "Opaque" "Queue" = "Transparent" }
        LOD 200
        Pass {
            Tags { "LightMode" = "ForwardBase" }
            Cull Back
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #include "RaymarchUtils.cginc"
/////////////////////
// BEGIN CODE
/////////////////////
float3 _translation_1;
float4 _rotation_1;

float _Width_1;

float _dist_1(float3 p) {
	float3 q = abs(p) - float3(_Width_1, _Width_1, _Width_1);
	return max(max(q.x, q.y), q.z);
}

float _dist_xform_1(float3 p) {
    return _dist_1(qrot(qinv(_rotation_1), p - _translation_1));
}
float3 _translation_2;
float4 _rotation_2;

float _Radius_1;

float _dist_2(float3 p) {
	return length(p) - _Radius_1;
}

float _dist_xform_2(float3 p) {
    return _dist_2(qrot(qinv(_rotation_2), p - _translation_2));
}
float3 _translation_3;
float4 _rotation_3;

float _Factor_1;

float _dist_3(float3 p) {
	float a = clamp(_Factor_1, 0.0, 1.0);
	return (1.0 - a) * _dist_xform_1(p) + a * _dist_xform_2(p);
}

float _dist_xform_3(float3 p) {
    return _dist_3(qrot(qinv(_rotation_3), p - _translation_3));
}

/////////////////////
// END CODE
/////////////////////
            #define _DIST_FUNCTION _dist_xform_3
            #define DO_LIGHTS 1
            #include "RaymarchMain.cginc"
            ENDCG
        }
    }
    FallBack "Diffuse"
}
