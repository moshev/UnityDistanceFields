Shader "Unlit/Mandelbulb" {
   Properties {
        _Power_1("Power", Float) = 2
        _Iterations_1("Iterations", Int) = 1
        _Bailout_1("Bailout limit", Float) = 2
        _Radius_1("Radius", Float) = 0.2
        _Height_1("Height", Float) = 1
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

float _Power_1;
int _Iterations_1;
float _Bailout_1;

float _dist_1(float3 p) {
	float3 z = p;
	float dr = 1.0f;
	float r = 0.0f;
	for (int i = 0; i < _Iterations_1; i++)
	{
		r = length(z);
		if (r > _Bailout_1) break;

		// convert to polar coordinates
		float theta = acos(z.z / r);
		float phi = atan2(z.y, z.x);
		dr = pow(r, _Power_1 - 1.0) * _Power_1 * dr + 1.0f;

		// scale and rotate the point
		float zr = pow(r, _Power_1);
		theta = theta * _Power_1;
		phi = phi * _Power_1;

		// convert back to cartesian coordinates
		z = zr * float3(sin(theta) * cos(phi), sin(phi) * sin(theta), cos(theta));
		z += p;
	}
	return 0.5 * log(r) * r / dr;
}

float _dist_xform_1(float3 p) {
    return _dist_1(qrot(qinv(_rotation_1), p - _translation_1));
}
float3 _translation_2;
float4 _rotation_2;

float _Radius_1;
float _Height_1;

float _dist_2(float3 p) {
    return lerp(length(p.xz) - _Radius_1, length(float3(p.x, abs(p.y) - _Height_1, p.z)) - _Radius_1,
            step(_Height_1, abs(p.y)));
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
