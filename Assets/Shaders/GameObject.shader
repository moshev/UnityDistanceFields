Shader "Unlit/GameObject" {
   Properties {
        _Start_1("Starting index", Float) = 0
        _End_1("Ending index", Float) = 2
        _Radius_1("GSDF Radius", Float) = 1
        _Exponent_1("GSDF Exponent", Float) = 8
        _OuterRadius_1("Outer Radius", Float) = 0.5
        _InnerRadius_1("Inner Radius", Float) = 0.15
        _R_1("Smooth union radius", Float) = 0.2
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

#ifndef GSDF_VECTORS_DEFINED
#define GSDF_VECTORS_DEFINED
static const float3 DIRECTIONS[19] = {
// cube face normals 0-2
float3(1.0, 0.0, 0.0),
float3(0.0, 1.0, 0.0),
float3(0.0, 0.0, 1.0),
// octahedron face normals 3-6
float3(0.5773502691896258, 0.5773502691896258, 0.5773502691896258),
float3(-0.5773502691896258, 0.5773502691896258, 0.5773502691896258),
float3(0.5773502691896258, -0.5773502691896258, 0.5773502691896258),
float3(0.5773502691896258, 0.5773502691896258, -0.5773502691896258),
// icosahedron faces 3-6+7-12
float3(0.0, 0.3568220897730899, 0.9341723589627157),
float3(0.0, -0.3568220897730899, 0.9341723589627157),
float3(0.9341723589627157, 0.0, 0.3568220897730899),
float3(-0.9341723589627157, 0.0, 0.3568220897730899),
float3(0.3568220897730899, 0.9341723589627157, 0.0),
float3(-0.3568220897730899, 0.9341723589627157, 0.0),
// dodecahedron 13-18
float3(0.0, 0.85065080835204, 0.5257311121191336),
float3(0.0, -0.85065080835204, 0.5257311121191336),
float3(0.5257311121191336, 0.0, 0.85065080835204),
float3(-0.5257311121191336, 0.0, 0.85065080835204),
float3(0.85065080835204, 0.5257311121191336, 0.0),
float3(-0.85065080835204, 0.5257311121191336, 0.0),
};
#endif

float _Start_1;
float _End_1;
float _Radius_1;
float _Exponent_1;

float _dist_1(float3 p) {
    int start = clamp((int)_Start_1, 0, 18);
    int end = clamp((int)_End_1, 0, 18);
    if (start > end) return 1000;
    if (_Exponent_1 >= 2) {
        // smooth object
        float distance = 0;
        for (int i = start; i <= end; i++) {
            distance += pow(abs(dot(p, DIRECTIONS[i])), _Exponent_1);
        }
        return pow(distance, 1/_Exponent_1) - _Radius_1;
    } else {
        // intersection of planes
        float distance = 0;
        for (int i = start; i <= end; i++) {
            distance = max(distance, abs(dot(p, DIRECTIONS[i])));
        }
        return distance - _Radius_1;
    }
}

float _dist_xform_1(float3 p) {
    return _dist_1(qrot(qinv(_rotation_1), p - _translation_1));
}
float3 _translation_2;
float4 _rotation_2;

float _OuterRadius_1;
float _InnerRadius_1;

float _dist_2(float3 p) {
    float z = p.y;
    float xy2 = dot(p, p);
    float b = _OuterRadius_1 - sqrt(xy2);
    return sqrt(b * b + z * z) - _InnerRadius_1;
}

float _dist_xform_2(float3 p) {
    return _dist_2(qrot(qinv(_rotation_2), p - _translation_2));
}
float3 _translation_3;
float4 _rotation_3;

float _R_1;

float _dist_3(float3 p) {
    float a = _dist_xform_1(p);
    float b = _dist_xform_2(p);
    if (!isfinite(a)) return b;
    if (!isfinite(b)) return a;
    float e = max(_R_1 - abs(a-b), 0);
    return min(a, b) - 0.25*e*e/_R_1;
}

/////////////////////
// END CODE
/////////////////////
            #define _DIST_FUNCTION _dist_3
            #define DO_LIGHTS 1
            #include "RaymarchMain.cginc"
            ENDCG
        }
    }
    FallBack "Diffuse"
}
