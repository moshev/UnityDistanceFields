Shader "Unlit/Mix" {
   Properties {
        _Radius_1("Radius", Float) = 1
        _Width_1("Width", Float) = 1
        _OuterRadius_1("Outer Radius", Float) = 0.5
        _InnerRadius_1("Inner Radius", Float) = 0.15
        _Factor_1("Mix Factor", Float) = 0.5
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

float _Radius_1;

float _dist_1(float3 p) {
	return length(p) - _Radius_1;
}

float _dist_xform_1(float3 p) {
    return _dist_1(p - _transform_1);
}
float3 _transform_2;

float _Width_1;

float _dist_2(float3 p) {
	float3 q = abs(p) - float3(_Width_1, _Width_1, _Width_1);
	return max(max(q.x, q.y), q.z);
}

float _dist_xform_2(float3 p) {
    return _dist_2(p - _transform_2);
}
float3 _transform_3;

float _dist_3(float3 p) {
	return max(_dist_xform_1(p), _dist_xform_2(p));
}

float _dist_xform_3(float3 p) {
    return _dist_3(p - _transform_3);
}
float3 _transform_4;

float _OuterRadius_1;
float _InnerRadius_1;

float _dist_4(float3 p) {
    float z = p.y;
    float xy2 = dot(p, p);
    float b = _OuterRadius_1 - sqrt(xy2);
    return sqrt(b * b + z * z) - _InnerRadius_1;
}

float _dist_xform_4(float3 p) {
    return _dist_4(p - _transform_4);
}
float3 _transform_5;

float _Factor_1;

float _dist_5(float3 p) {
	float a = clamp(_Factor_1, 0.0, 1.0);
	return (1.0 - a) * _dist_xform_3(p) + a * _dist_xform_4(p);
}

float _dist_xform_5(float3 p) {
    return _dist_5(p - _transform_5);
}

/////////////////////
// END CODE
/////////////////////
            #define _DIST_FUNCTION _dist_xform_5
            #include "RaymarchMain.cginc"
            ENDCG
        }
    }
    FallBack "Diffuse"
}
