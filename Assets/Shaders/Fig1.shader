Shader "Unlit/Fig1" {
   Properties {
        _Sx_1("Width", Float) = 1
        _Sy_1("Height", Float) = 1
        _Sz_1("Depth", Float) = 1
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

float _dist_xform_1(float3 p) {
    return _dist_1(qrot(qinv(_rotation_1), p - _translation_1));
}

/////////////////////
// END CODE
/////////////////////
            #define _DIST_FUNCTION _dist_xform_1
            #define DO_LIGHTS 1
            #include "RaymarchMain.cginc"
            ENDCG
        }
    }
    FallBack "Diffuse"
}
