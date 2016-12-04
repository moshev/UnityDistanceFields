Shader "Unlit/Raymarch2" {
	Properties {
		_Color ("Color", Color) = (1,1,1,1)
		_SphereRadius ("Radius", Range(0.01, 0.5)) = 0.2
		_Mix ("Mix", Range(0.01, 0.99)) = 0
	}
	SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 200
		Pass {
		Cull Back
		CGPROGRAM
		#pragma vertex vert
		#pragma fragment frag
		
		#include "UnityCG.cginc"
		#include "DistanceEstimators.cginc"

		struct appdata {
			float4 vertex : POSITION;
		};
		
		struct v2f {
			float4 vertex : SV_POSITION;
			float3 objpos : TEXCOORD0;
		};
		
		v2f vert(appdata input) {
			v2f o;
			o.vertex = UnityObjectToClipPos(input.vertex);
			o.objpos = input.vertex.xyz;
			return o;
		}
		
		fixed4 _Color;
		
		struct raycontext {
			float3 p;
			float3 dir;
		};
		
		struct rayresult {
			float3 p;
			float3 n;
			float distance;
		};
		
		float3x3 rotOf(float4x4 m) {
			float3 r0 = m[0].xyz;
			float3 r1 = m[1].xyz;
			float3 r2 = m[2].xyz;
			return float3x3(r0, r1, r2);
		}
		
		float _SphereRadius;
		float _Mix;
		float distToObject(float3 p) {
			float a = clamp(0, 1, _Mix);
			float b = clamp(0, 1, 1 - _Mix);
			return a * distTorus(p, float3(0,0,0), float3(0,1,0), 0.3, 0.1) + b * distCube(p, 0, 0.4);
			//return a * distSphere(p, float3(0,0,0), _SphereRadius) + b * distCube(p, 0, 0.4);
			//return distSphere(p, float3(0,0,0), 0.3);
		}
		
		#define EPSILON 0.001
		
		float3 grad(float3 p) {
			float3 ex = float3(EPSILON, 0, 0);
			float3 ey = float3(0, EPSILON, 0);
			float3 ez = float3(0, 0, EPSILON);
			return float3(
				distToObject(p + ex) - distToObject(p - ex),
				distToObject(p + ey) - distToObject(p - ey),
				distToObject(p + ez) - distToObject(p - ez)
			);
		}
		
		#define MAXITER 128
		struct marchresult {
			float3 p;
			float distance;
		};
		marchresult march(float3 p, float3 dir) {
			float t = 0;
			float d = distToObject(p);
			float3 q = p;
			for (int i = 0; i < MAXITER; i++) {
				if (abs(d) < EPSILON) break;
				t += d;
				q = p + t * dir;
				d = distToObject(q);
			}
			marchresult mres;
			mres.p = q;
			mres.distance = d;
			return mres;
		}
		
		rayresult trace(float3 objpoint) {
			float3 dir = -normalize(ObjSpaceViewDir(float4(objpoint, 1)));
			float4x4 w2o = unity_WorldToObject;
			float4x4 o2w = unity_ObjectToWorld;
			rayresult res;
			marchresult mres = march(objpoint, dir);
			res.p = mres.p;
			res.n = -grad(mres.p);
			res.distance = mres.distance;
			return res;
		}

		struct output {
			fixed4 color : SV_Target;
			float depth : SV_Depth;
		};

		output frag(v2f input) {
			rayresult res;
			res = trace(input.objpos);
			if (abs(res.distance) > EPSILON) {
				discard;
			}
			float4 screenp = UnityObjectToClipPos(res.p);
			output o;
			o.color = float4(0.5, 0.5, 0, 1);
			o.depth = screenp.z / screenp.w;
			/*
			//o.color = fixed4(res.n * 0.5 + 0.5, 1);
			//float3 sp = res.screenp.xyz / res.screenp.w;
			//float depth = (res.screenp.z / res.screenp.w - _ProjectionParams.y) / (_ProjectionParams.z - _ProjectionParams.y);
			//float depth = sp.z * 0.1;
			//float depth = _ProjectionParams.y;
			float4 sp1 = UnityObjectToClipPos(input.objpos);
			float4 sp2 = mul(UNITY_MATRIX_P, input.worldpos);
			float d1 = sp1.z / sp1.w;
			float d2 = sp2.z / sp2.w;
			float dd = 100*(d2 - d1);
			//o.color = abs(sp.z) < 0.1 ? fixed4(1,0,0,1) : fixed4(0,1,0,1);
			//o.color = fixed4(depth, depth, depth, 1);
			o.color = fixed4(dd, dd, dd, 1);
			//o.color = fixed4(input.worldpos.xy, 0, 1);
			//o.color = fixed4(sp * 0.5 + 0.5, 1);
			//float near = _ProjectionParams.y;
			//float far = _ProjectionParams.z;
			//o.depth = input.vertex.z / input.vertex.w - 1;
			o.depth = d1;
			*/
			return o;
		}

		ENDCG
		}
	}
	FallBack "Diffuse"
}
