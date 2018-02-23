#include "UnityLightingCommon.cginc" 

struct appdata {
	float4 vertex: POSITION;
};

struct v2f {
	float4 vertex: SV_POSITION;
	float3 objpos: TEXCOORD0;
	float3 original: TEXCOORD1;
};

float _CanvasSize;
v2f vert(appdata input) {
	v2f o;
	const float4x4 mTM = transpose(UNITY_MATRIX_M);
	const float4x4 mMV = UNITY_MATRIX_MV;
	const float4x4 mTMV = transpose(mMV);
	const float4x4 mV = transpose(UNITY_MATRIX_I_V);
	const float4x4 mITMV = UNITY_MATRIX_IT_MV;
	const float3 centre = mTM[3].xyz;
	const float3 forward = normalize(_WorldSpaceCameraPos - centre);
	const float3 cUp = mV[1].xyz;
	const float3 right = normalize(cross(forward, cUp));
	const float3 up = normalize(cross(right, forward));
	float3 v = _CanvasSize * input.vertex.x * right + _CanvasSize * input.vertex.y * up + _CanvasSize * forward;
	v += centre;
	o.vertex = mul(UNITY_MATRIX_P, mul(UNITY_MATRIX_V, float4(v, 1)));
	o.objpos = mul(unity_WorldToObject, float4(v, 1));
	o.original = input.vertex.xyz;
	return o;
}

float3x3 rotOf(float4x4 m) {
	float3 r0 = m[0].xyz;
	float3 r1 = m[1].xyz;
	float3 r2 = m[2].xyz;
	return float3x3(r0, r1, r2);
}

float distToObject(float3 p) {
	return _DIST_FUNCTION(p);
}

#define EPSILON 0.001

float3 grad(float3 p) {
	float3 ex = float3(EPSILON, 0, 0);
	float3 ey = float3(0, EPSILON, 0);
	float3 ez = float3(0, 0, EPSILON);
	return float3(
		distToObject(p + ex) - distToObject(p - ex),
		distToObject(p + ey) - distToObject(p - ey),
		distToObject(p + ez) - distToObject(p - ez));
}

#define MAXITER 256
struct marchresult {
	float3 p; // Intersection point in object space
    float3 n; // Normal in object space
	float distance; // Distance to object at p
};
// Raymarch algorithm. If intersection is found, distance will be less than EPSILON
marchresult march(float3 p, float3 dir) {
	float t = 0;
	float d = distToObject(p);
	float3 q = p;
	for (int i = 0; i < MAXITER; i++) {
		if (abs(d) < EPSILON)
			break;
		t += d;
		q = p + t * dir;
		d = distToObject(q);
	}
	marchresult mres;
	mres.p = q;
	mres.distance = d;
	return mres;
}

struct output {
	fixed4 color: SV_Target;
	float depth: SV_Depth;
};

output frag(v2f input) {
	output o;
	marchresult res;
	float3 dir = -normalize(ObjSpaceViewDir(float4(input.objpos, 1)));
	res = march(input.objpos, dir);
	if (!isfinite(res.distance) || abs(res.distance) > EPSILON) {
		discard;
	} else {
		float4 screenp = UnityObjectToClipPos(res.p);
#ifdef DO_LIGHTS
        fixed3 dir = _WorldSpaceLightPos0.xyz;
        if (_WorldSpaceLightPos0.w > 0.5) dir = res.p - dir;
        fixed diff = max (0, dot (res.n, dir));
        fixed4 c;
        c.rgb = fixed3(1.0,0.796,0.247) * _LightColor0 * diff;
        c.a = 1;
        o.color = c;
#else
		o.color = float4(abs(res.n), 1);
#endif
		o.depth = screenp.z / screenp.w;
	}
	return o;
}
