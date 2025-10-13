Shader "Custom/Outline"
{
    Properties
    {
        _MainTex ("Base (RGB)", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)
        _OutlineColor ("Outline Color", Color) = (0,0,0,1)
        _OutlineWidth ("Outline Width", Range(0.001, 0.03)) = 0.01
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }

        // Pass para el contorno
        Pass
        {
            Name "Outline"
            Cull Front
            ZWrite On
            ZTest LEqual
            ColorMask RGB

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
            };

            float _OutlineWidth;
            float4 _OutlineColor;

            v2f vert (appdata v)
            {
                v2f o;
                float3 norm = normalize(v.normal);
                float3 pos = v.vertex.xyz + norm * _OutlineWidth;
                o.pos = UnityObjectToClipPos(float4(pos, 1.0));
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                return _OutlineColor;
            }
            ENDCG
        }

        // Pass normal del objeto
        Pass
        {
            Name "Base"
            Tags { "LightMode"="ForwardBase" }

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _Color;

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                return tex2D(_MainTex, i.uv) * _Color;
            }
            ENDCG
        }
    }
}
