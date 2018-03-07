Shader "Surface/TeapotTess" {
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
    float b = 0.43 - sqrt(xy2);
    return sqrt(b * b + z * z) - -0.16;
}

float _dist_xform_1(float3 p) {
    return _dist_1(qrot(qinv(float4(0,0,0,1)), p - float3(0,-0.581,0)));
}



float _dist_2(float3 p) {
	return length(p) - 1.46;
}

float _dist_xform_2(float3 p) {
    return _dist_2(qrot(qinv(float4(0,0,0,1)), p - float3(0,0.154,0)));
}



float _dist_3(float3 p) {
	float a = clamp(0.52, 0.0, 1.0);
	return (1.0 - a) * _dist_xform_1(p) + a * _dist_xform_2(p);
}




float _dist_4(float3 p) {
    return lerp(length(p.xz) - 0.1, length(float3(p.x, abs(p.y) - 1, p.z)) - 0.1,
            step(1, abs(p.y)));
}

float _dist_xform_3(float3 p) {
    return _dist_4(qrot(qinv(float4(0,0,-0.280507,0.959852)), p - float3(1.261,0.572,0)));
}



float _dist_5(float3 p) {
    float a = _dist_3(p);
    float b = _dist_xform_3(p);
    if (!isfinite(a)) return b;
    if (!isfinite(b)) return a;
    float e = max(0.1 - abs(a-b), 0);
    return min(a, b) - 0.25*e*e/0.1;
}





float _dist_6(float3 p) {
    //float x = max(p.x - float3(3.56*0.5, 0, 0),-p.x - float3(3.56*0.5, 0, 0));
    //float y = max(p.y - float3(2.27*0.5, 0, 0),-p.y - float3(2.27*0.5, 0, 0));
    //float z = max(p.z - float3(1.99*0.5, 0, 0),-p.z - float3(1.99*0.5, 0, 0));
    float x = max(p.x - 3.56*0.5, -p.x - 3.56*0.5);
    float y = max(p.y - 2.27*0.5, -p.y - 2.27*0.5);
    float z = max(p.z - 1.99*0.5, -p.z - 1.99*0.5);
    float d = x;
    d = max(d,y);
    d = max(d,z);
    return d;
}

float _dist_xform_4(float3 p) {
    return _dist_6(qrot(qinv(float4(0,0,0,1)), p - float3(-0.8559999,-0.831,0)));
}





float _dist_7(float3 p) {
    //float x = max(p.x - float3(1.68*0.5, 0, 0),-p.x - float3(1.68*0.5, 0, 0));
    //float y = max(p.y - float3(1.54*0.5, 0, 0),-p.y - float3(1.54*0.5, 0, 0));
    //float z = max(p.z - float3(1.8*0.5, 0, 0),-p.z - float3(1.8*0.5, 0, 0));
    float x = max(p.x - 1.68*0.5, -p.x - 1.68*0.5);
    float y = max(p.y - 1.54*0.5, -p.y - 1.54*0.5);
    float z = max(p.z - 1.8*0.5, -p.z - 1.8*0.5);
    float d = x;
    d = max(d,y);
    d = max(d,z);
    return d;
}

float _dist_xform_5(float3 p) {
    return _dist_7(qrot(qinv(float4(0,0,0,1)), p - float3(1.256,-0.334,0)));
}

float _dist_8(float3 p) {
    float a = _dist_xform_4(p);
    float b = _dist_xform_5(p);
    if (!isfinite(a)) return b;
    if (!isfinite(b)) return a;
    return min(a, b);
}



float _dist_9(float3 p) {
    float a = _dist_5(p);
    float b = _dist_8(p);
    if (!isfinite(a)) return b;
    if (!isfinite(b)) return a;
    float e = max(0.03 - abs(a-b), 0);
    return max(a, b) + 0.25*e*e/0.03;
}




float _dist_10(float3 p) {
    float z = p.y;
    float xy2 = dot(p, p);
    float b = 0.42 - sqrt(xy2);
    return sqrt(b * b + z * z) - 0.07;
}

float _dist_xform_6(float3 p) {
    return _dist_10(qrot(qinv(float4(-0.7071068,0,0,0.7071068)), p - float3(-0.5469999,-0.22,0)));
}



float _dist_11(float3 p) {
	return length(p) - 0.95;
}

float _dist_xform_7(float3 p) {
    return _dist_11(qrot(qinv(float4(0,0,0,1)), p - float3(0.2650003,-0.4,0)));
}

float _dist_12(float3 p) {
    float a = _dist_xform_6(p);
    float b = _dist_xform_7(p);
    return max(a, -b);
}



float _dist_13(float3 p) {
    float a = _dist_9(p);
    float b = _dist_12(p);
    if (!isfinite(a)) return b;
    if (!isfinite(b)) return a;
    float e = max(0.03 - abs(a-b), 0);
    return min(a, b) - 0.25*e*e/0.03;
}

/////////////////////
// END CODE
/////////////////////
        #define _DIST_FUNCTION _dist_13
        #include "TessMain.cginc"
        ENDCG
    }
    FallBack "Diffuse"
}
