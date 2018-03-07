Shader "Unlit/Teapot" {
   Properties {
        _OuterRadius_1("body1: Outer Radius", Float) = 0.5
        _InnerRadius_1("body1: Inner Radius", Float) = 0.15
        _Radius_1("body2: Radius", Float) = 1
        _Factor_1("bodymix: Mix Factor", Float) = 0.5
        _Radius_2("spout: Radius", Float) = 0.2
        _Height_1("spout: Height", Float) = 1
        _R_1("bodyspout: Smooth union radius", Float) = 0.2
        _Sx_1("limit1: Width", Float) = 1
        _Sy_1("limit1: Height", Float) = 1
        _Sz_1("limit1: Depth", Float) = 1
        _Sx_2("limit2: Width", Float) = 1
        _Sy_2("limit2: Height", Float) = 1
        _Sz_2("limit2: Depth", Float) = 1
        _R_2("bodycut: Smooth intersection radius", Float) = 0.2
        _OuterRadius_2("handletorus: Outer Radius", Float) = 0.5
        _InnerRadius_2("handletorus: Inner Radius", Float) = 0.15
        _Radius_3("handlecutout: Radius", Float) = 1
        _R_3("Teapot: Smooth union radius", Float) = 0.2
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
float3 _translation_4;
float4 _rotation_4;

float _Radius_2;
float _Height_1;

float _dist_4(float3 p) {
    return lerp(length(p.xz) - _Radius_2, length(float3(p.x, abs(p.y) - _Height_1, p.z)) - _Radius_2,
            step(_Height_1, abs(p.y)));
}

float _dist_xform_3(float3 p) {
    return _dist_4(qrot(qinv(_rotation_4), p - _translation_4));
}
float3 _translation_5;
float4 _rotation_5;

float _R_1;

float _dist_5(float3 p) {
    float a = _dist_3(p);
    float b = _dist_xform_3(p);
    if (!isfinite(a)) return b;
    if (!isfinite(b)) return a;
    float e = max(_R_1 - abs(a-b), 0);
    return min(a, b) - 0.25*e*e/_R_1;
}
float3 _translation_6;
float4 _rotation_6;

float _Sx_1;
float _Sy_1;
float _Sz_1;

float _dist_6(float3 p) {
    //float x = max(p.x - float3(_Sx_1*0.5, 0, 0),-p.x - float3(_Sx_1*0.5, 0, 0));
    //float y = max(p.y - float3(_Sy_1*0.5, 0, 0),-p.y - float3(_Sy_1*0.5, 0, 0));
    //float z = max(p.z - float3(_Sz_1*0.5, 0, 0),-p.z - float3(_Sz_1*0.5, 0, 0));
    float x = max(p.x - _Sx_1*0.5, -p.x - _Sx_1*0.5);
    float y = max(p.y - _Sy_1*0.5, -p.y - _Sy_1*0.5);
    float z = max(p.z - _Sz_1*0.5, -p.z - _Sz_1*0.5);
    float d = x;
    d = max(d,y);
    d = max(d,z);
    return d;
}

float _dist_xform_4(float3 p) {
    return _dist_6(qrot(qinv(_rotation_6), p - _translation_6));
}
float3 _translation_7;
float4 _rotation_7;

float _Sx_2;
float _Sy_2;
float _Sz_2;

float _dist_7(float3 p) {
    //float x = max(p.x - float3(_Sx_2*0.5, 0, 0),-p.x - float3(_Sx_2*0.5, 0, 0));
    //float y = max(p.y - float3(_Sy_2*0.5, 0, 0),-p.y - float3(_Sy_2*0.5, 0, 0));
    //float z = max(p.z - float3(_Sz_2*0.5, 0, 0),-p.z - float3(_Sz_2*0.5, 0, 0));
    float x = max(p.x - _Sx_2*0.5, -p.x - _Sx_2*0.5);
    float y = max(p.y - _Sy_2*0.5, -p.y - _Sy_2*0.5);
    float z = max(p.z - _Sz_2*0.5, -p.z - _Sz_2*0.5);
    float d = x;
    d = max(d,y);
    d = max(d,z);
    return d;
}

float _dist_xform_5(float3 p) {
    return _dist_7(qrot(qinv(_rotation_7), p - _translation_7));
}
float3 _translation_8;
float4 _rotation_8;

float _dist_8(float3 p) {
    float a = _dist_xform_4(p);
    float b = _dist_xform_5(p);
    if (!isfinite(a)) return b;
    if (!isfinite(b)) return a;
    return min(a, b);
}
float3 _translation_9;
float4 _rotation_9;

float _R_2;

float _dist_9(float3 p) {
    float a = _dist_5(p);
    float b = _dist_8(p);
    if (!isfinite(a)) return b;
    if (!isfinite(b)) return a;
    float e = max(_R_2 - abs(a-b), 0);
    return max(a, b) + 0.25*e*e/_R_2;
}
float3 _translation_10;
float4 _rotation_10;

float _OuterRadius_2;
float _InnerRadius_2;

float _dist_10(float3 p) {
    float z = p.y;
    float xy2 = dot(p, p);
    float b = _OuterRadius_2 - sqrt(xy2);
    return sqrt(b * b + z * z) - _InnerRadius_2;
}

float _dist_xform_6(float3 p) {
    return _dist_10(qrot(qinv(_rotation_10), p - _translation_10));
}
float3 _translation_11;
float4 _rotation_11;

float _Radius_3;

float _dist_11(float3 p) {
	return length(p) - _Radius_3;
}

float _dist_xform_7(float3 p) {
    return _dist_11(qrot(qinv(_rotation_11), p - _translation_11));
}
float3 _translation_12;
float4 _rotation_12;

float _dist_12(float3 p) {
    float a = _dist_xform_6(p);
    float b = _dist_xform_7(p);
    return max(a, -b);
}
float3 _translation_13;
float4 _rotation_13;

float _R_3;

float _dist_13(float3 p) {
    float a = _dist_9(p);
    float b = _dist_12(p);
    if (!isfinite(a)) return b;
    if (!isfinite(b)) return a;
    float e = max(_R_3 - abs(a-b), 0);
    return min(a, b) - 0.25*e*e/_R_3;
}

/////////////////////
// END CODE
/////////////////////
            #define _DIST_FUNCTION _dist_13
            #define DO_LIGHTS 1
            #include "RaymarchMain.cginc"
            ENDCG
        }
    }
    FallBack "Diffuse"
}
