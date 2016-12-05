/*
 * right parallelopiped
 */
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

/*
 * proper cube
 */
float distCube(float3 p, float3 c, float r) {
	float3 vr = float3(r, r, r);
	return distCube(p, c, vr);
}

/*
 * sphere
 */
float distSphere(float3 p, float3 c, float r) {
	return distance(c, p) - r;
}

/*
 * cone
 * c - centre of base
 * n - "up" normal
 * r - base radius
 * h - height
 */
float distCone(float3 p, float3 c, float3 n, float r, float h) {
	float cosphi = h / sqrt(r*r + h*h);
	float3 cp = p - c;
	float dot_n_cp = dot(n, cp);
	float dheight = sqrt(dot(cp, cp) - dot_n_cp*dot_n_cp);
	float radius = r * abs(h - dot_n_cp) / h;
	float dcone = (dheight - radius) * cosphi;
	//dcone = min(length(cp - h * n), dcone);
	return max(-dot_n_cp, max(dcone, dot_n_cp - h + 0.001));
}

/*
 * torus
 * c - centre
 * n - normal
 * rc - radius to centre of tube
 * rt - radius of tube
 */
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

/* 
 * cylinder with spherical caps at ends
 * a, b - centres of the caps, r - radius
 */
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

/*
 * Paraboloid with equation y = a(x**2 + z**2)
 */
float distParaboloid(float3 p, float3 c, float a) {
	p -= c;
	float z = a * (p.x*p.x + p.z*p.z);
	float2 ox = normalize(p.xz);
	float2 p0 = float2(sqrt(p.x*p.x + p.z*p.z), p.z);
	float2 pClosest = float2(p0.x, z);
	float dy = 2 * a * p0.x;
	float result = (z - p.y) * sqrt(1.0/(1.0 + dy*dy));
	if (!isfinite(result) || result > 1000.0) return 1000.0;
	else return result;
}
