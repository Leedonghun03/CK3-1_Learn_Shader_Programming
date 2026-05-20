Shader "Unlit/ToonShaderOutline"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _LingWidth ("LineWidth", Range(0,1)) = 0.15
        _Color ("Color", Color) = (1,1,1,1)
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
                float3 mOutline : TEXCOORD2;
            };

            sampler2D _MainTex;
            float _LingWidth;
            float4 _Color;

            v2f vert (appdata v)
            {
                v2f o;

                o.pos = mul(UNITY_MATRIX_M, v.vertex);
                
                float3 WorldPositon = o.pos;
                //float3 lightDir = WorldPositon - _WorldSpaceLightPos0.xyz; // when using spot light
                float3 lightDir = normalize (_WorldSpaceLightPos0.xyz); // when using Direction light
                lightDir = normalize(lightDir);

                o.pos = mul(UNITY_MATRIX_V, o.pos);
                o.pos = mul(UNITY_MATRIX_P, o.pos);

                float3 worldNormal = mul((float3x3)UNITY_MATRIX_M, v.mNormal);
                worldNormal = normalize(worldNormal);
                o.mDiffuse = dot(lightDir, worldNormal);

                float3 ViewDirect = normalize(_WorldSpaceCameraPos - WorldPositon);
                o.mOutline = dot(ViewDirect, normalize(worldNormal));

                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float3 diffuse = saturate(i.mDiffuse);

                diffuse = _Color * ceil(diffuse * 5) / 5.0f;

                if (i.mOutline.x < _LingWidth)
                    diffuse = float3(0.0, 0.0, 0.0);

                return float4 (diffuse, 1);
            }
            ENDCG
        }
    }
}
