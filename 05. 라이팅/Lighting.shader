Shader "Unlit/Lighting"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _SpecPower ("SpecPower", Range(1, 100)) = 20
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 mNormal: NORMAL;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 mDiffuse : TEXCOORD1;
                float3 mViewDir: TEXCOORD2;
                float3 mReflection: TEXCOORD3;
            };

            sampler2D _MainTex;
            float _SpecPower;

            v2f vert (appdata v)
            {
                v2f o;

                o.pos = mul(UNITY_MATRIX_M, v.vertex);

                //float3 lightDir = o.pos.xyz - _WorldSpaceLightPos0.xyz; // when using spot light
                float3 lightDir = normalize (_WorldSpaceLightPos0.xyz); // when using Direction light
                lightDir = normalize(lightDir);

                float3 viewDir = normalize(o.pos.xyz - _WorldSpaceCameraPos.xyz);
                o.mViewDir = viewDir;

                o.pos = mul(UNITY_MATRIX_V, o.pos);
                o.pos = mul(UNITY_MATRIX_P, o.pos);

                float3 worldNormal = mul((float3x3)UNITY_MATRIX_M, v.mNormal);
                worldNormal = normalize(worldNormal);

                o.mDiffuse = dot(lightDir, worldNormal);
                o.mReflection = reflect(lightDir, worldNormal);

                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float3 diffuse = saturate(i.mDiffuse);

                float3 reflection = normalize(i.mReflection);
                float3 viewDir = normalize(i.mViewDir);
                float3 specular = 0;
                
                if (diffuse.x > 0)
                {
                    specular = saturate(dot(reflection, viewDir));
                    specular = pow(specular, _SpecPower);
                }

                float3 ambient = float3(0.1f, 0.1f, 0.1f);

                return float4 (ambient + diffuse + specular, 1);
            }
            ENDCG
        }
    }
}
