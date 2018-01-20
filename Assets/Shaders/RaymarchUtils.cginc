// rotate vector by quaternion
float3 qrot(float4 q, float3 v) {
	float3x3 m = float3x3(
	1-2*(q.z*q.z+q.w*q.w), 2*(q.y*q.z-q.w*q.x), 2*(q.y*q.w+q.z*q.x),
	2*(q.y*q.z+q.w*q.x), 1-2*(q.y*q.y+q.w*q.w), 2*(q.z*q.w-q.y*q.x),
	2*(q.y*q.w-q.z*q.x), 2*(q.z*q.w+q.y*q.x), 1-2*(q.y*q.y+q.z*q.z)
	);
	return mul(m, v);
}

// inverse rotation
float4 qinv(float4 q) {
	return float4(q.x, -q.yzw);
}
