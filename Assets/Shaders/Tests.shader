Shader "Unlit/Tests" {
   Properties {
        _Iterations_1("Iterations", Int) = 16
        _Cx_1("C x", Float) = 0.6651410179788055
        _Cy_1("C y", Float) = 0.995165511524831
        _Cz_1("C z", Float) = -0.09786148786209856
        _Cw_1("C w", Float) = 0.42337454445066547
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

int _Iterations_1;
float _Cx_1;
float _Cy_1;
float _Cz_1;
float _Cw_1;

float _dist_1(float3 p) {
    float4 c = float4(_Cx_1, _Cy_1, _Cz_1, _Cw_1);
    float4 z = float4(p,0.0);
    float md2 = 1.0;
    float mz2 = dot(z,z);

    for( int i=0; i<_Iterations_1; i++ )
    {
        // |dz|^2 -> 4*|dz|^2
        md2 *= 4.0*mz2;
        
        // z -> z2 + c
        z = float4( z.x*z.x-dot(z.yzw,z.yzw), 2.0*z.x*z.yzw ) + c;

        mz2 = dot(z,z);
        if(mz2>4.0) break;
    }
    
    return 0.25*sqrt(mz2/md2)*log(mz2);
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
