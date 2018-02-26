struct raycontext {
	float3 p;
	float3 dir;
};

struct rayresult {
	float3 p;
	float3 n;
	float distance;
};

#define EPSILON 0.0001

float3 grad(float3 p) {
	float3 ex = float3(EPSILON, 0, 0);
	float3 ey = float3(0, EPSILON, 0);
	float3 ez = float3(0, 0, EPSILON);
	return float3(
		_DIST_FUNCTION(p + ex) - _DIST_FUNCTION(p - ex),
		_DIST_FUNCTION(p + ey) - _DIST_FUNCTION(p - ey),
		_DIST_FUNCTION(p + ez) - _DIST_FUNCTION(p - ez));
}

int maxIters=256;
struct marchresult {
	float3 p;
	float distance;
};
marchresult march(float3 p, float3 dir) {
	marchresult mres;
    if (dot(dir, dir) < 0.5) {
        mres.p = 0;
        mres.distance = 1000;
    }
	float t = 0;
	float d = _DIST_FUNCTION(p);
	float3 q = p;
	for (int i = 0; i < maxIters; i++) {
		if (abs(d) < EPSILON)
			break;
		t += d;
		q = p + t * dir;
		d = _DIST_FUNCTION(q);
	}
	mres.p = q;
	mres.distance = d;
	return mres;
}

rayresult trace(raycontext ctx) {
    rayresult res;
    if (dot(ctx.dir, ctx.dir) < 0.5) {
        res.p = 0;
        res.n = 0;
        res.distance = 1000;
    }
	marchresult mres = march(ctx.p, ctx.dir);
	res.p = mres.p;
	res.n = normalize(grad(mres.p));
	res.distance = mres.distance;
	return res;
}
