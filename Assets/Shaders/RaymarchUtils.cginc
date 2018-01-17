// rotate vector by quaternion
float3 qrot(float4 q, float3 v) {
	float3x3 m = float3x3(
		1 - 2*q.z*q.z - 2*q.w*q.w, 0 + 2*q.y*q.z + 2*q.w*q.x, 0 + 2*q.y*q.w + 2*q.z*q.x,
		0 + 2*q.z*q.y + 2*q.w*q.x, 1 - 2*q.y*q.y - 2*q.w*q.w, 0 - 2*q.y*q.x + 2*q.z*q.w,
		0 - 2*q.z*q.x + 2*q.w*q.y, 0 + 2*q.y*q.x + 2*q.w*q.z, 1 - 2*q.y*q.y - 2*q.z*q.z
	);
	return mul(m, v);
}

// inverse rotation
float4 qinv(float4 q) {
	return float4(q.x, -q.yzw);
}
