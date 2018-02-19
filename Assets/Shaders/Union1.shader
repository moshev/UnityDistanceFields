Shader "Unlit/Union1" {
   Properties {
        _OuterRadius_1("Outer Radius", Float) = 0.5
        _InnerRadius_1("Inner Radius", Float) = 0.15
        _Sx_1("Width", Float) = 1
        _Sy_1("Height", Float) = 1
        _Sz_1("Depth", Float) = 1
        _R_1("Smooth union radius", Float) = 0.2
        _Radius_1("Radius", Float) = 0.2
        _Height_1("Height", Float) = 1
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

float _OuterRadius_1;
float _InnerRadius_1;

float _dist_1(float3 p) {
    float z = p.y;
    float xy2 = dot(p, p);
    float b = _OuterRadius_1 - sqrt(xy2);
    return sqrt(b * b + z * z) - _InnerRadius_1;
}

float _dist_xform_1(float3 p) {
    return _dist_1(qrot(qinv(_rotation_1), p - _translation_1));
}
float3 _translation_2;
float4 _rotation_2;

float _Sx_1;
float _Sy_1;
float _Sz_1;

float _dist_2(float3 p) {
    float x = max(p.x - float3(_Sx_1*0.5, 0, 0),-p.x - float3(_Sx_1*0.5, 0, 0));
    float y = max(p.y - float3(_Sy_1*0.5, 0, 0),-p.y - float3(_Sy_1*0.5, 0, 0));
    float z = max(p.z - float3(_Sz_1*0.5, 0, 0),-p.z - float3(_Sz_1*0.5, 0, 0));
    float d = x;
    d = max(d,y);
    d = max(d,z);
    return d;
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

float _dist_xform_3(float3 p) {
    return _dist_3(qrot(qinv(_rotation_3), p - _translation_3));
}
float3 _translation_4;
float4 _rotation_4;

float _Radius_1;
float _Height_1;

float _dist_4(float3 p) {
    return lerp(length(p.xz) - _Radius_1, length(float3(p.x, abs(p.y) - _Height_1, p.z)) - _Radius_1,
            step(_Height_1, abs(p.y)));
}

float _dist_xform_4(float3 p) {
    return _dist_4(qrot(qinv(_rotation_4), p - _translation_4));
}
float3 _translation_5;
float4 _rotation_5;

float _dist_5(float3 p) {
    float a = _dist_xform_3(p);
    float b = _dist_xform_4(p);
    return max(a, -b);
}

float _dist_xform_5(float3 p) {
    return _dist_5(qrot(qinv(_rotation_5), p - _translation_5));
}

/////////////////////
// END CODE
/////////////////////
            #define _DIST_FUNCTION _dist_xform_5
            #define DO_LIGHTS 1
            #include "RaymarchMain.cginc"
            ENDCG
        }
    }
    FallBack "Diffuse"
}
