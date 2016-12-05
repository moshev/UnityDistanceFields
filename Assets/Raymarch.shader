Shader "Custom/Raymarch" {
	Properties {
		_Mix ("Mix Coefficient", Range(0, 1)) = 0
		_Tess ("Tesselation", Range(1,32)) = 4
		_Color ("Color", color) = (1,1,1,0)
		_Phi ("Phi", Range(0, 1)) = 0
		_Theta ("Theta", Range(0,1)) = 0
	}
	SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 300

		CGPROGRAM
		#pragma target 4.6
		//#pragma surface surf BlinnPhong addshadow fullforwardshadows vertex:disp tessellate:tessFixed nolightmap
		#pragma surface surf Standard addshadow vertex:vert tessellate:tessFixed nolightmap
		//#pragma surface surf Standard vertex:vert
		#include "DistanceEstimators.cginc"

		float _Tess;
		float4 tessFixed() {
			float t = _Tess;
			float s = sqrt(_Tess);
			return _Tess;
		}

		float _Mix;
		float _Phi;
		float _Theta;
		float distObject(float3 p) {
			float a = clamp(0, 1, _Mix);
			float b = clamp(0, 1, 1 - _Mix);
			//return a * distCapsule(p, float3(0, -0.7, 0), float3(0, 0.7, 0), 0.5) + b * distCube(p, 0, 0.5);
			//return a * distTorus(p, float3(0,0,0), float3(0,1,0), 1.0, 0.3) + b * distCube(p, 0, 0.5);
			float sp = sin(_Phi * UNITY_PI);
			float cp = cos(_Phi * UNITY_PI);
			float st = sin(_Theta * UNITY_TWO_PI);
			float ct = cos(_Theta * UNITY_TWO_PI);
			float3 coneN = float3(st * cp, st * sp, ct);
			return a * distCone(p, -0.5 * coneN, coneN, 0.6, 1.5) + b * distCube(p, 0, 0.65);
		}

		float _Displacement;

		#define MAXITER 16
		#define EPS 0.0001

		float3 gradient(float3 p) {
			float3 ex = float3(EPS, 0, 0);
			float3 ey = float3(0, EPS, 0);
			float3 ez = float3(0, 0, EPS);
			return float3(
				distObject(p + ex) - distObject(p - ex),
				distObject(p + ey) - distObject(p - ey),
				distObject(p + ez) - distObject(p - ez));
		}

		void vert (inout appdata_full v) {
			float3 p = v.vertex.xyz;
			float3 n = normalize(p); //v.normal;
			float d = distObject(p);
			float t = 0;
			for (int i = 0; i < MAXITER; i++) {
				if (abs(d) < EPS) {
					break;
				}
				t += d;
				p -= d * n;
				d = distObject(p);
				if (abs(d) < 100 * EPS) {
					//n = normalize(gradient(p));
				}
			}
			v.vertex = float4(p, 1);
			float3 g = gradient(p);
			//v.normal = float3(1,1,1);//normalize(g);
			v.normal = float3(0, 0, 1);
			v.tangent = float4(1, 0, 0, 1);
			v.color = float4(p, 1);
		}

		fixed4 _Color;

		struct Input {
			float4 color : COLOR;
		};

		void surf (Input IN, inout SurfaceOutputStandard o) {
			half4 c = _Color;
			o.Albedo = c.rgb;
			//float3 n = -normalize(gradient(IN.color.yzw));
			float3 n = normalize(gradient(IN.color.xyz));
			o.Normal = n;
			//o.Albedo = abs(n);
		}
		ENDCG
	}
}
