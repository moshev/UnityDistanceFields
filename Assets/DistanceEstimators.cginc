/*cube with 3 lengths*/
float distCube(float3 p, float3 c, float3 vr) {
	float3 bmin = c - vr;
	float3 bmax = c + vr;
	float3 dmin = bmin - p;
	float3 dmax = p - bmax;
	float3 max1 = max(dmin, dmax);
	float result = max1.x;
	result = max(max1.y, result);
	result = max(max1.z, result);
	return result;
}

/*proper cube*/
float distCube(float3 p, float3 c, float r) {
	float3 vr = float3(r, r, r);
	return distCube(p, c, vr);
}

/*sphere*/
float distSphere(float3 p, float3 c, float r) {
	return distance(c, p) - r;
}

/*torus*/
/*rc - radius to centre of tube*/
/*rt - radius of tube*/
float distTorus(float3 p, float3 c, float3 n, float rc, float rt) {
    // equation is
    // (rmax - sqrt(dot(p.xy))) ** 2 + z**2 - rmin**2
    // for torus symmetric around z
    float z = dot(p, n) - dot(c, n);
    float3 p1 = p - z * n;
    float xy2 = dot(p1 - c, p1 - c);
    float b = rc - sqrt(xy2);
    return sqrt(b * b + z * z) - rt;
}

float distCylinderx(float3 p, float3 c, float h, float r) {
    float3 q = p - c;
    return max(max(-h - q.x, q.x - h), sqrt(dot(q.yz, q.yz)) - r);
}

float distCylindery(float3 p, float3 c, float h, float r) {
    float3 q = p - c;
    return max(max(-h - q.y, q.y - h), sqrt(dot(q.xz, q.xz)) - r);
}

/*cylinder with spherical caps at ends*/
/* a, b - centres of the caps, r - radius */
float distCapsule(float3 p, float3 a, float3 b, float r) {
    float3 n = normalize(b - a);
    float3 p1 = p - a;
    float d = dot(n, p1);
    float3 c = d * n;
    if (dot(n, c) < 0.0f) {
        return distSphere(p, a, r);
    }
    if (dot(n, c) > distance(a, b)) {
        return distSphere(p, b, r);
    }
    float daxis = length(p1 - d * n);
    return daxis - r;
}
