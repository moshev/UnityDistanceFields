Shader "Unlit/Mandelbulb" {
   Properties {
        _Power_1("Mandelbulb: Power", Float) = 2
        _Iterations_1("Mandelbulb: Iterations", Float) = 16
        _Bailout_1("Mandelbulb: Bailout limit", Float) = 2
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
float _Iterations_1;
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
