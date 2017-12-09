struct raycontext {
	float3 p;
	float3 dir;
};

struct rayresult {
	float3 p;
	float3 n;
	float distance;
};

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

#define MAXITER 256
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

rayresult trace(raycontext ctx) {
	marchresult mres = march(ctx.p, ctx.dir);
	rayresult res;
	res.p = mres.p;
	res.n = -normalize(grad(mres.p));
	res.distance = mres.distance;
	return res;
}
