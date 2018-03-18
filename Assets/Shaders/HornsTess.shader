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
        #pragma surface surf Standard addshadow fullforwardshadows vertex:disp tessellate:tess nolightmap
        #pragma target 4.6
        #include "Tessellation.cginc"
        #include "RaymarchUtils.cginc"
/////////////////////
// BEGIN CODE
/////////////////////




float _dist_1(float3 p) {
    float z = p.y;
    float xy2 = dot(p, p);
    float b = 0.6 - sqrt(xy2);
    return sqrt(b * b + z * z) - 0.13;
}

float _dist_xform_1(float3 p) {
    return _dist_1(qrot(qinv(float4(0,0,0,1)), p - float3(0.01200008,0,0)));
}





float _dist_2(float3 p) {
    //float x = max(p.x - float3(1*0.5, 0, 0),-p.x - float3(1*0.5, 0, 0));
    //float y = max(p.y - float3(1*0.5, 0, 0),-p.y - float3(1*0.5, 0, 0));
    //float z = max(p.z - float3(1*0.5, 0, 0),-p.z - float3(1*0.5, 0, 0));
    float x = max(p.x - 1*0.5, -p.x - 1*0.5);
    float y = max(p.y - 1*0.5, -p.y - 1*0.5);
    float z = max(p.z - 1*0.5, -p.z - 1*0.5);
    float d = x;
    d = max(d,y);
    d = max(d,z);
    return d;
}

float _dist_xform_2(float3 p) {
    return _dist_2(qrot(qinv(float4(0,0,0,1)), p - float3(0.5290003,0,0)));
}



float _dist_3(float3 p) {
	return length(p) - 1;
}

float _dist_xform_3(float3 p) {
    return _dist_3(qrot(qinv(float4(0,0,0,1)), p - float3(-0.1409998,0,0)));
}

float _dist_4(float3 p) {
    float a = _dist_xform_2(p);
    float b = _dist_xform_3(p);
    if (!isfinite(a)) return b;
    if (!isfinite(b)) return a;
	return max(a, b);
}



float _dist_5(float3 p) {
	float a = clamp(0.3, 0.0, 1.0);
	return (1.0 - a) * _dist_xform_1(p) + a * _dist_4(p);
}

/////////////////////
// END CODE
/////////////////////
        #define _DIST_FUNCTION _dist_5
        #include "TessMain.cginc"
        ENDCG
    }
    FallBack "Diffuse"
}
