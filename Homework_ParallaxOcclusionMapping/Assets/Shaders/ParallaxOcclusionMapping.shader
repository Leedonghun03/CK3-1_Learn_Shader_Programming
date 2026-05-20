Shader "Homework/Parallax Occlusion Mapping"
{
    Properties
    {
        _MainTex ("Albedo", 2D) = "white" {}
        _BumpMap ("Normal Map", 2D) = "bump" {}
        _HeightMap ("Height Map", 2D) = "black" {}
        _Color ("Tint", Color) = (1, 1, 1, 1)
        _HeightScale ("Height Scale", Range(0, 0.12)) = 0.045
        _MinLayers ("Min Layers", Range(4, 32)) = 8
        _MaxLayers ("Max Layers", Range(8, 96)) = 48
        _POMSpecColor ("Specular Color", Color) = (0.25, 0.25, 0.25, 1)
        _Gloss ("Gloss", Range(8, 128)) = 48
        _EdgeTrim ("Discard Outside UV", Range(0, 1)) = 1
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 300

        Pass
        {
            Tags { "LightMode"="ForwardBase" }

            CGPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fwdbase

            #include "UnityCG.cginc"
            #include "Lighting.cginc"
            #include "AutoLight.cginc"

            sampler2D _MainTex;
            sampler2D _BumpMap;
            sampler2D _HeightMap;
            float4 _MainTex_ST;
            fixed4 _Color;
            float _HeightScale;
            float _MinLayers;
            float _MaxLayers;
            fixed4 _POMSpecColor;
            float _Gloss;
            float _EdgeTrim;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float4 tangent : TANGENT;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 viewDirTS : TEXCOORD1;
                float3 lightDirTS : TEXCOORD2;
                LIGHTING_COORDS(3, 4)
            };

            float3x3 BuildWorldToTangentMatrix(appdata v)
            {
                float3 normalWorld = UnityObjectToWorldNormal(v.normal);
                float3 tangentWorld = UnityObjectToWorldDir(v.tangent.xyz);
                float tangentSign = v.tangent.w * unity_WorldTransformParams.w;
                float3 bitangentWorld = cross(normalWorld, tangentWorld) * tangentSign;
                return float3x3(tangentWorld, bitangentWorld, normalWorld);
            }

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);

                float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                float3x3 worldToTangent = BuildWorldToTangentMatrix(v);
                float3 worldViewDir = UnityWorldSpaceViewDir(worldPos);
                float3 worldLightDir = UnityWorldSpaceLightDir(worldPos);

                o.viewDirTS = mul(worldToTangent, worldViewDir);
                o.lightDirTS = mul(worldToTangent, worldLightDir);
                TRANSFER_VERTEX_TO_FRAGMENT(o);
                return o;
            }

            float2 ParallaxOcclusionUV(float2 uv, float3 viewDirTS)
            {
                viewDirTS = normalize(viewDirTS);
                float ndotv = saturate(abs(viewDirTS.z));
                float layerCount = lerp(_MaxLayers, _MinLayers, ndotv);
                float layerDepth = 1.0 / layerCount;
                float currentLayerDepth = 0.0;
                float2 parallaxVector = (viewDirTS.xy / max(abs(viewDirTS.z), 0.12)) * _HeightScale;
                float2 deltaUV = parallaxVector / layerCount;

                float2 currentUV = uv;
                float currentDepth = 1.0 - tex2D(_HeightMap, currentUV).r;

                [loop]
                while (currentLayerDepth < currentDepth)
                {
                    currentUV -= deltaUV;
                    currentDepth = 1.0 - tex2D(_HeightMap, currentUV).r;
                    currentLayerDepth += layerDepth;
                }

                float2 previousUV = currentUV + deltaUV;
                float afterDepth = currentDepth - currentLayerDepth;
                float beforeDepth = (1.0 - tex2D(_HeightMap, previousUV).r) - currentLayerDepth + layerDepth;
                float weight = afterDepth / max(afterDepth - beforeDepth, 0.0001);
                return lerp(currentUV, previousUV, saturate(weight));
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float3 viewDirTS = normalize(i.viewDirTS);
                float2 uv = ParallaxOcclusionUV(i.uv, viewDirTS);

                if (_EdgeTrim > 0.5)
                {
                    clip(uv.x);
                    clip(uv.y);
                    clip(1.0 - uv.x);
                    clip(1.0 - uv.y);
                }

                fixed4 albedo = tex2D(_MainTex, uv) * _Color;
                float3 normalTS = UnpackNormal(tex2D(_BumpMap, uv));
                float3 lightDirTS = normalize(i.lightDirTS);
                float3 halfDirTS = normalize(lightDirTS + viewDirTS);

                fixed attenuation = LIGHT_ATTENUATION(i);
                fixed ndotl = saturate(dot(normalTS, lightDirTS));
                fixed specular = pow(saturate(dot(normalTS, halfDirTS)), _Gloss) * ndotl;

                fixed3 ambient = UNITY_LIGHTMODEL_AMBIENT.rgb * albedo.rgb;
                fixed3 diffuse = albedo.rgb * _LightColor0.rgb * ndotl * attenuation;
                fixed3 spec = _POMSpecColor.rgb * _LightColor0.rgb * specular * attenuation;
                return fixed4(ambient + diffuse + spec, albedo.a);
            }
            ENDCG
        }
    }

    FallBack "Bumped Diffuse"
}
