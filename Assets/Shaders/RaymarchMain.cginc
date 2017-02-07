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

float distToObject(float3 p) {
	return _DIST_FUNCTION(p);
}

#define EPSILON 0.0001

float3 grad(float3 p) {
	float3 ex = float3(EPSILON, 0, 0);
	float3 ey = float3(0, EPSILON, 0);
	float3 ez = float3(0, 0, EPSILON);
	return float3(
		distToObject(p + ex) - distToObject(p - ex),
		distToObject(p + ey) - distToObject(p - ey),
		distToObject(p + ez) - distToObject(p - ez));
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
	fixed4 color: SV_Target;
	float depth: SV_Depth;
};

output frag(v2f input) {
	output o;
	rayresult res;
	res = trace(input.objpos);
	if (!isfinite(res.distance) || abs(res.distance) > EPSILON) {
		discard;
	} else {
		float4 screenp = UnityObjectToClipPos(res.p);
		o.color = float4(abs(res.n), 1);
		o.depth = screenp.z / screenp.w;
	}
	return o;
}
