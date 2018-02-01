// rotate vector by quaternion
float3 qrot(float4 q, float3 v) {
	float3x3 m = float3x3(
	1-2*(q.y*q.y+q.z*q.z), 2*(q.x*q.y-q.z*q.w), 2*(q.x*q.z+q.y*q.w),
	2*(q.x*q.y+q.z*q.w), 1-2*(q.x*q.x+q.z*q.z), 2*(q.y*q.z-q.x*q.w),
	2*(q.x*q.z-q.y*q.w), 2*(q.y*q.z+q.x*q.w), 1-2*(q.x*q.x+q.y*q.y)
	);
	return mul(m, v);
}

// inverse rotation
float4 qinv(float4 q) {
	return float4(-q.xyz, q.w);
}
