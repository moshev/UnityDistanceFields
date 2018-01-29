Shader "Surface/HornsTess" {
   Properties {
        _EdgeLength ("Tessellation edge Length", Range(2,50)) = 15
        _MaxDisplacement ("Maximum object-space displacement", Range(0,1)) = 0.1
        _MainTex ("Main texture", 2D) = "white" {}
        _Color ("Color", color) = (1,1,1,0)
        _Specular ("Specular", Range(0,1)) = 0.5
    }
    SubShader {
        Tags { "RenderType" = "Opaque" }
        LOD 200
        CGPROGRAM
        #pragma surface surf BlinnPhong addshadow fullforwardshadows vertex:disp tessellate:tess nolightmap
        #pragma target 4.6
        #include "Tessellation.cginc"
        #include "RaymarchUtils.cginc"
/////////////////////
// BEGIN CODE
/////////////////////



float _dist_1(float3 p) {
	return length(p) - 2.12;
}

float _dist_xform_1(float3 p) {
    return _dist_1(qrot(qinv(float4(0,0,0,1)), p - float3(0,0,0)));
}



float _dist_2(float3 p) {
	float3 q = abs(p) - float3(1.86, 1.86, 1.86);
	return max(max(q.x, q.y), q.z);
}

float _dist_xform_2(float3 p) {
    return _dist_2(qrot(qinv(float4(-0.2460241,0.07082517,-0.2674241,0.9289458)), p - float3(1.398,0.587,2.525)));
}

float _dist_3(float3 p) {
	return max(_dist_xform_1(p), _dist_xform_2(p));
}

float _dist_xform_3(float3 p) {
    return _dist_3(qrot(qinv(float4(0,0,0,1)), p - float3(0,0,0)));
}




float _dist_4(float3 p) {
    float z = p.y;
    float xy2 = dot(p, p);
    float b = 1.18 - sqrt(xy2);
    return sqrt(b * b + z * z) - 0.38;
}

float _dist_xform_4(float3 p) {
    return _dist_4(qrot(qinv(float4(0,0,0,1)), p - float3(0,0,0)));
}



float _dist_5(float3 p) {
	float a = clamp(0.77, 0.0, 1.0);
	return (1.0 - a) * _dist_xform_3(p) + a * _dist_xform_4(p);
}

float _dist_xform_5(float3 p) {
    return _dist_5(qrot(qinv(float4(0,0,0,1)), p - float3(0,0,0)));
}

/////////////////////
// END CODE
/////////////////////
        #define _DIST_FUNCTION _dist_xform_5
        #include "TessMain.cginc"
        ENDCG
    }
    FallBack "Diffuse"
}
