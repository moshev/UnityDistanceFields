Shader "Unlit/Raymarch3" {
	Properties {
		_Color ("Color", Color) = (1,1,1,1)
	}
	SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 200
		Pass {
		Cull Off
		CGPROGRAM
		#pragma vertex vert
		#pragma fragment frag
		
		#include "UnityCG.cginc"

		struct appdata {
			float4 vertex : POSITION;
		};
		
		struct v2f {
			float4 vertex : SV_POSITION;
			float3 worldpos : TEXCOORD0;
		};
		
		v2f vert(appdata input) {
			v2f o;
			o.vertex = UnityObjectToClipPos(input.vertex);
			o.worldpos = mul(UNITY_MATRIX_MV, input.vertex);
			return o;
		}
		
		fixed4 _Color;
		
		struct output {
			fixed4 color : SV_Target;
			float depth : SV_Depth;
		};

		output frag(v2f input) {
			output o;
			float4 projected = mul(UNITY_MATRIX_P, input.worldpos);
			o.color = fixed4(0.5, 0.5, 0.5, 1);
			o.depth = projected.z / projected.w;
			return o;
		}

		ENDCG
		}
	}
	FallBack "Diffuse"
}
