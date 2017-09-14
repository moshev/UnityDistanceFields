﻿Shader "Unlit/Fullscene" {
    Properties{
        _Color("Color", Color) = (1,1,1,1)
        _SphereRadius("Radius", Range(0.01, 0.5)) = 0.2
        _Mix("Mix", Range(0.0, 1.0)) = 0
    }
        SubShader{
        Tags{ "RenderType" = "Opaque" }
        LOD 200
        Pass{
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
        float3 original : TEXCOORD1;
    };

    v2f vert(appdata input) {
        v2f o;
        const float4x4 mTM = transpose(UNITY_MATRIX_M);
        const float4x4 mMV = UNITY_MATRIX_MV;
        const float4x4 mTMV = transpose(mMV);
        const float4x4 mV = transpose(UNITY_MATRIX_I_V);
        const float4x4 mITMV = UNITY_MATRIX_IT_MV; // mul(UNITY_MATRIX_M, UNITY_MATRIX_I_V);
        const float3 centre = mTM[3].xyz;
        const float3 forward = normalize(_WorldSpaceCameraPos - centre);
        const float3 cUp = mV[1].xyz;
        const float3 right = normalize(cross(forward, cUp));
        const float3 up = normalize(cross(right, forward));
        o.vertex = float4(-input.vertex.x, input.vertex.y, input.vertex.z, 1);
        //o.vertex = mul(UNITY_MATRIX_P, mul(UNITY_MATRIX_MV, input.vertex));
        o.objpos = o.vertex;
        o.original = input.vertex.xyz;
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
    float distMyParaboloid(float3 p) {
        //return max(p.y - 0.45, distParaboloid(p, float3(0, -0.49, 0), 6));
        //return distParaboloid(p, float3(0, -0.49, 0), 6);
        //return max(p.y - 0.45, abs(distParaboloid(p, float3(0, -0.49, 0), 6)) - 0.03);
        //return max(distCube(p, 0, 0.48), -distParaboloid(p, float3(0, -0.48, 0), 6));
        return distParaboloid(float3(p.x,-1 - p.y,p.z), float3(0, 0, 0), 6);
    }
    float distToObject(float3 p) {
        float a = clamp(0, 1, _Mix);
        float b = clamp(0, 1, 1 - _Mix);
        //return a * distTorus(p, float3(0,0,0), float3(0,1,0), 0.3, 0.1) + b * distCube(p, 0, 0.4);
        //return a * distTorus(p, float3(0,0,0), float3(0,1,0), 0.3, 0.1) + b * distSphere(p, float3(0,0,0), _SphereRadius);
        return a * distSphere(p, float3(0,0,0), _SphereRadius) + b * distCube(p, 0, 0.4);
        //return distSphere(p, float3(0,0,0), 0.3);
        //return a * distMyParaboloid(p) + b * distSphere(p, float3(0,0,0), _SphereRadius);
        //return a * distMyParaboloid(p) + b * distTorus(p, float3(0,0,0), float3(0,1,0), 0.3, 0.1);
    }

#define EPSILON 0.0001

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
        res.n = -normalize(grad(mres.p));
        res.distance = mres.distance;
        return res;
    }

    struct output {
        fixed4 color : SV_Target;
        float depth : SV_Depth;
    };

    output frag(v2f input) {
        output o;
        rayresult res;
#define MODE 1
#if MODE == 0
        res = trace(input.objpos);
        if (!isfinite(res.distance) || abs(res.distance) > EPSILON) {
            discard;
        }
        else
        {
            float4 screenp = UnityObjectToClipPos(res.p);
            //o.color = _Color;//float4(0.5, 0.5, 0, 1);
            o.color = float4(abs(res.n), 1);
            o.depth = screenp.z / screenp.w;
        }
#elif MODE == 1
        //float4 screenp = UnityObjectToClipPos(input.objpos);
        o.depth = 1;// screenp.z / screenp.w;
        o.color = fixed4(abs(normalize(ObjSpaceViewDir(float4(input.objpos, 1)))), 1);
#elif MODE == 2
        float4 screenp = UnityObjectToClipPos(input.objpos);
        o.depth = 0.5;// screenp.z / screenp.w;
        o.color = fixed4(normalize(_WorldSpaceCameraPos - UNITY_MATRIX_MV[3].xyz), 1);
#elif MODE == 3
        float4 screenp = UnityObjectToClipPos(input.objpos);
        o.depth = screenp.z / screenp.w;
        o.color = fixed4((UNITY_MATRIX_V)[0].xyz, 1);
#elif MODE == 4
        float4 screenp = UnityObjectToClipPos(input.objpos);
        o.depth = screenp.z / screenp.w;
        const float4x4 m = UNITY_MATRIX_M;
        const float4x4 tm = transpose(m);
        o.color = fixed4(abs(tm[3].xyz), 1);
#elif MODE == 5
        const float4x4 mTM = transpose(UNITY_MATRIX_M);
        const float4x4 mMV = UNITY_MATRIX_MV;
        const float4x4 mTMV = transpose(mMV);
        const float4x4 mV = transpose(UNITY_MATRIX_I_V);
        const float4x4 mITMV = UNITY_MATRIX_IT_MV; // mul(UNITY_MATRIX_M, UNITY_MATRIX_I_V);
        const float3 centre = mTM[3].xyz;
        const float3 forward = normalize(_WorldSpaceCameraPos - centre);
        const float3 cUp = mV[1].xyz;
        const float3 right = normalize(cross(forward, cUp));
        const float3 up = normalize(cross(right, forward));
        float3 v = input.original.x * right + input.original.y * up;// +2 * forward;// -cForward;
        float4 screenp = UnityObjectToClipPos(input.objpos);
        o.depth = screenp.z / screenp.w;
        o.color = fixed4((v), 1);
#endif
        return o;
    }

    ENDCG
    }
    }
        FallBack "Diffuse"
}