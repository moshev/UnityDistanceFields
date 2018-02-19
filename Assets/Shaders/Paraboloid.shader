Shader "Unlit/Paraboloid" {
   Properties {
        _Sx_1("Width", Float) = 1
        _Sy_1("Height", Float) = 1
        _Sz_1("Depth", Float) = 1
        _A_1("X multiplier", Float) = 1
        _B_1("Z multiplier", Float) = 1
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

float _Sx_1;
float _Sy_1;
float _Sz_1;

float _dist_1(float3 p) {
    float x = max(p.x - float3(_Sx_1*0.5, 0, 0),-p.x - float3(_Sx_1*0.5, 0, 0));
    float y = max(p.y - float3(_Sy_1*0.5, 0, 0),-p.y - float3(_Sy_1*0.5, 0, 0));
    float z = max(p.z - float3(_Sz_1*0.5, 0, 0),-p.z - float3(_Sz_1*0.5, 0, 0));
    float d = x;
    d = max(d,y);
    d = max(d,z);
    return d;
}

float _dist_xform_1(float3 p) {
    return _dist_1(qrot(qinv(_rotation_1), p - _translation_1));
}
float3 _translation_2;
float4 _rotation_2;

float _A_1;
float _B_1;

float _dist_2(float3 p) {
    float2 ab = float2(_A_1, _B_1);
    float3 n = float3(2 * _A_1 * p.x, -1, 2 * _B_1 * p.z);
    float3 pest;
	for (int i = 0; i < 10; i++) {
        float A = dot(ab, n.xz*n.xz);
        float B = 2 * dot(ab, p.xz*n.xz) - n.y;
        float C = dot(ab, p.xz*p.xz) - p.y;
        float D = B * B - 4 * A * C;
        float T = -0.5 * B / A;
        if (D < 0) return 1;
        D = 0.5 * sqrt(D) / A;
        if (T * D < 0) T += D;
        else T -= D;
        pest = p + T * n;
        n = float3(2 * _A_1 * pest.x, -1, 2 * _B_1 * pest.z);
	}
    float3 dp = pest - p;
    float dist = length(dp);
    if (dot(dp, n) > 0) dist = -dist;
    return dist;
}


float _dist_xform_2(float3 p) {
    return _dist_2(qrot(qinv(_rotation_2), p - _translation_2));
}
float3 _translation_3;
float4 _rotation_3;

float _dist_3(float3 p) {
    float a = _dist_xform_1(p);
    float b = _dist_xform_2(p);
    if (!isfinite(a)) return b;
    if (!isfinite(b)) return a;
	return max(a, b);
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
