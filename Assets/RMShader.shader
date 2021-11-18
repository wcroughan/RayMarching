Shader "Unlit/RTShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _MaxDist ("Max Distance", float) = 100
        _MaxSteps ("Max Steps", int) = 100
        _SurfaceTolerance ("Surface Tolerance", float) = 0.001
        _smoothK ("Smoothing K", float) = 0.5
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            // Upgrade NOTE: excluded shader from DX11, OpenGL ES 2.0 because it uses unsized arrays
            #pragma exclude_renderers d3d11 gles
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            float _MaxDist;
            int _MaxSteps;
            float _SurfaceTolerance;
            float _smoothK;

            int _NumSpheres;
            float _SphereRadii[128];
            float4 _SpherePositions[128];

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float3 hitPos : TEXCOORD1;
                float3 r0 : TEXCOORD2;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.hitPos = v.vertex;
                o.r0 = mul(unity_WorldToObject, float4(_WorldSpaceCameraPos, 1));
                return o;
            }

            float smoothMin(float dis[128], float k)
            {
                float mn = _MaxDist;
                float mn2 = _MaxDist;
                float mn3 = _MaxDist;
                float mn4 = _MaxDist;
                for (int i = 0; i < _NumSpheres; i++){
                    if (mn > dis[i]) {
                        mn4 = mn3;
                        mn3 = mn2;
                        mn2 = mn;
                        mn = dis[i];
                    } else if (mn2 > dis[i]) {
                        mn4 = mn3;
                        mn3 = mn2;
                        mn2 = dis[i];
                    } else if (mn3 > dis[i]) {
                        mn4 = mn3;
                        mn3 = dis[i];
                    } else if (mn4 > dis[i]) {
                        mn4 = dis[i];
                    }

                }
                float h = max(k - (mn4-mn), 0) / k;
                return mn - h*h*h*k*1/6.0;
            }

            float GetDist(float3 p) {
                float dis[128];
                for (int i = 0; i < _NumSpheres; i++)
                    dis[i] = length(p - _SpherePositions[i].xyz) - _SphereRadii[i];
                return smoothMin(dis, _smoothK);

                // float d1 = length(p - _SphereCenter) - _SphereRadius;
                // float d2 = length(p - _SphereCenter2) - _SphereRadius2;
                // return min(d1, d2);
                // return smoothMin(d1, d2, _smoothK);
                // return length(p) - 0.5;
            }

            float RayMarch(float3 p0, float3 dir) {
                float fullDist = 0;
                float3 p = p0;

                for (int i = 0; i < _MaxSteps; i++) {
                    float d = GetDist(p);
                    fullDist += d;

                    if (fullDist > _MaxDist || d < _SurfaceTolerance)
                        break;

                    p = p0 + dir * fullDist;
                }

                return fullDist;
            }

            float3 GetNormal(float3 p) {
                float2 e = float2(1e-2, 0);
                float3 n = GetDist(p) - float3(
                    GetDist(p-e.xyy),
                    GetDist(p-e.yxy),
                    GetDist(p-e.yyx)
                );
                return normalize(n);
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 uv = i.uv - 0.5;
                float3 r0 = i.r0; //float3(0, 0, -3);
                float3 rd = normalize(i.hitPos - r0); // normalize(float3(uv, 1));
                
                float d = RayMarch(r0, rd);
                fixed4 col = 0;

                if (d < _MaxDist) {
                    // col.r = 1;
                    float3 p = r0 + rd * d;
                    float3 n = GetNormal(p);
                    col.rg = n.xy;
                    col.b = 1 - d / 2;
                }

                return col;
            }
            ENDCG
        }
    }
}
