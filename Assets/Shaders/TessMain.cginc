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

#define NUMSAMPLES 4

struct appdata {
	float4 vertex: POSITION;
	float4 tangent: TANGENT;
	float3 normal: NORMAL;
	float2 texcoord: TEXCOORD0;
};

float _EdgeLength;
float _MaxDisplacement;

float4 tess(appdata v0, appdata v1, appdata v2) {
	return UnityEdgeLengthBasedTessCull(v0.vertex, v1.vertex, v2.vertex, _EdgeLength, _MaxDisplacement);
}

void disp(inout appdata v) {
	float3 p = v.vertex.xyz;
	float3 n = v.normal;
	float d = distToObject(p);
	float t = 0;
	for (int i = 0; i <= NUMSAMPLES; i++) {
		//if (abs(d) < EPSILON) break;
		t += d;
		p = -t * n + v.vertex.xyz;
		d = distToObject(p);
	}
	if (abs(t) > _MaxDisplacement) t = sign(t) * _MaxDisplacement;
	p = -t * n + v.vertex.xyz;
	v.vertex = float4(p, 1);
	v.normal = normalize(grad(p));
}

struct Input {
	float2 uv_MainTex;
};

sampler2D _MainTex;
fixed4 _Color;
float _Specular;

void surf (Input IN, inout SurfaceOutput o) {
	half4 c = tex2D (_MainTex, IN.uv_MainTex) * _Color;
	o.Albedo = c.rgb;
	o.Specular = _Specular;
	o.Gloss = 1.0;
}