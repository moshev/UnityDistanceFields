Shader "Unlit/Mandelbulb-Sphere" {
   Properties {
        _Power_1("Power", Float) = 2
        _Iterations_1("Iterations", Int) = 1
        _Bailout_1("Bailout limit", Float) = 2
        _Radius_1("Radius", Float) = 1
        _CanvasSize("CanvasSize", Float) = 1
    }
    SubShader {
        Tags { "RenderType" = "Opaque" }
        LOD 200
        Pass {
            Cull Back
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
/////////////////////
// BEGIN CODE
/////////////////////
float3 _transform_1;

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
    return _dist_1(p - _transform_1);
}
float3 _transform_2;

float _Radius_1;

float _dist_2(float3 p) {
	return length(p) - _Radius_1;
}

float _dist_xform_2(float3 p) {
    return _dist_2(p - _transform_2);
}
float3 _transform_3;

float _dist_3(float3 p) {
	return max(_dist_xform_1(p), _dist_xform_2(p));
}

float _dist_xform_3(float3 p) {
    return _dist_3(p - _transform_3);
}

/////////////////////
// END CODE
/////////////////////
            #define _DIST_FUNCTION _dist_xform_3
            #include "RaymarchMain.cginc"
            ENDCG
        }
    }
    FallBack "Diffuse"
}
