Shader "Custom/TorusTesselationShader" {
	Properties {
		_Color ("Color", Color) = (1,1,1,1)
                _Tess("Tesselation", Range(0,32)) = 4
        }
	SubShader {
                    Tags{ "RenderType" = "Opaque" }
                    LOD 300

                    CGPROGRAM
#pragma target 4.6
                    //#pragma surface surf BlinnPhong addshadow fullforwardshadows vertex:disp tessellate:tessFixed nolightmap
#pragma surface surf Standard addshadow vertex:vert tessellate:tessDist nolightmap
                    //#pragma surface surf Standard vertex:vert
#include "DistanceEstimators.cginc"
#include "Tessellation.cginc"

                    float _Tess;
                float4 tessFixed() {
                    float t = _Tess;
                    float s = sqrt(_Tess);
                    return _Tess;
                }

                float4 tessDist(appdata_full v0, appdata_full v1, appdata_full v2) {
                    if (_Tess < 1) return float4(1, 1, 1, 1);
                    float minDist = 0.1;
                    float maxDist = 25.0;
                    //return UnityDistanceBasedTess(v0.vertex, v1.vertex, v2.vertex, minDist, maxDist, _Tess);
                    return UnityEdgeLengthBasedTess(v0.vertex, v1.vertex, v2.vertex, 128.0f / _Tess);
                }

                float distObject(float3 p) {
                    return distTorus(p, float3(0,0,0), float3(0,1,0), 1.0, 0.4);
                }

#define MAXITER 16
#define EPS 0.0001

                float3 gradient(float3 p) {
                    float3 ex = float3(EPS, 0, 0);
                    float3 ey = float3(0, EPS, 0);
                    float3 ez = float3(0, 0, EPS);
                    return float3(
                        distObject(p + ex) - distObject(p - ex),
                        distObject(p + ey) - distObject(p - ey),
                        distObject(p + ez) - distObject(p - ez));
                }

                void vert(inout appdata_full v) {
                    float3 p = v.vertex.xyz;
                    float3 n = normalize(v.normal); //v.normal;
                    float d = distObject(p);
                    float t = 0;
                    for (int i = 0; i < MAXITER; i++) {
                        if (abs(d) < EPS) {
                            break;
                        }
                        t += d;
                        p -= d * n;
                        d = distObject(p);
                        if (abs(d) < 100 * EPS) {
                            //n = normalize(gradient(p));
                        }
                    }
                    v.vertex = float4(p, 1);
                    float3 g = gradient(p);
                    //v.normal = float3(1,1,1);//normalize(g);
                    v.normal = float3(0, 0, 1);
                    v.tangent = float4(1, 0, 0, 1);
                    v.color = float4(p, 1);
                }

                fixed4 _Color;

                struct Input {
                    float4 color : COLOR;
                };

                void surf(Input IN, inout SurfaceOutputStandard o) {
                    half4 c = _Color;
                    o.Albedo = c.rgb;
                    //float3 n = -normalize(gradient(IN.color.yzw));
                    float3 n = normalize(gradient(IN.color.xyz));
                    o.Normal = n;
                    //o.Albedo = abs(n);
                }
                ENDCG
                }
	FallBack "Diffuse"
}
