Shader "Charles Will Code It/PointShader"
{
	SubShader
        {
            Pass
            {
        CGPROGRAM
#pragma target 5.0
#pragma vertex vert
#pragma fragment frag
#include "UnityCG.cginc"
        struct data
        {
            float3 pos;
        };
        StructuredBuffer<data> buf_Points;
        float3 _worldPos;
        struct ps_input
        {
            float4 pos: SV_POSITION;
            float3 color: COLOR0;
        };

        ps_input vert(uint id: SV_VertexID)
        {
            ps_input result;
            float3 worldPos = buf_Points[id].pos + _worldPos;
            result.pos = mul(UNITY_MATRIX_VP, float4(worldPos, 1.0f));
            result.color = worldPos;
            return result;
        }

        float4 frag(ps_input result): COLOR
        {
            float4 col = float4(result.color, 1.0f);
            if (col.x <= 0.0f && col.y <= 0.0f && col.z <= 0.0f)
            {
                col.xyz = float3(0.0f, 1.0f, 1.0f);
            }

            return col;
        }
        ENDCG

            }
	}
}
