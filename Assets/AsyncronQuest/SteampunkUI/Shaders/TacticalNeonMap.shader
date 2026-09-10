Shader "UI/TacticalNeonMap"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _EdgeColor ("Neon Green Edge", Color) = (0.0, 1.0, 0.4, 1.0)
        _BackgroundColor ("Background Color", Color) = (0.005, 0.015, 0.010, 0.98)
        _EdgeThreshold ("Edge Sensitivity", Range(0.01, 0.8)) = 0.18
        _EdgeGlow ("Edge Glow Intensity", Range(1.0, 5.0)) = 2.4

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "Default"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord  : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            fixed4 _Color;
            fixed4 _EdgeColor;
            fixed4 _BackgroundColor;
            float _EdgeThreshold;
            float _EdgeGlow;

            v2f vert(appdata_t v)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.worldPosition = v.vertex;
                OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);
                OUT.texcoord = v.texcoord;
                OUT.color = v.color * _Color;
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                float2 uv = IN.texcoord;
                float2 d = _MainTex_TexelSize.xy * 1.5;

                // Campionamento kernel 3x3 Sobel
                float3 c00 = tex2D(_MainTex, uv + float2(-d.x, -d.y)).rgb;
                float3 c01 = tex2D(_MainTex, uv + float2( 0.0, -d.y)).rgb;
                float3 c02 = tex2D(_MainTex, uv + float2( d.x, -d.y)).rgb;
                float3 c10 = tex2D(_MainTex, uv + float2(-d.x,  0.0)).rgb;
                float3 c11 = tex2D(_MainTex, uv).rgb;
                float3 c12 = tex2D(_MainTex, uv + float2( d.x,  0.0)).rgb;
                float3 c20 = tex2D(_MainTex, uv + float2(-d.x,  d.y)).rgb;
                float3 c21 = tex2D(_MainTex, uv + float2( 0.0,  d.y)).rgb;
                float3 c22 = tex2D(_MainTex, uv + float2( d.x,  d.y)).rgb;

                // Calcolo gradiente di luminanza
                float lum00 = dot(c00, float3(0.299, 0.587, 0.114));
                float lum01 = dot(c01, float3(0.299, 0.587, 0.114));
                float lum02 = dot(c02, float3(0.299, 0.587, 0.114));
                float lum10 = dot(c10, float3(0.299, 0.587, 0.114));
                float lum12 = dot(c12, float3(0.299, 0.587, 0.114));
                float lum20 = dot(c20, float3(0.299, 0.587, 0.114));
                float lum21 = dot(c21, float3(0.299, 0.587, 0.114));
                float lum22 = dot(c22, float3(0.299, 0.587, 0.114));

                float gx = -lum00 + lum02 - 2.0 * lum10 + 2.0 * lum12 - lum20 + lum22;
                float gy = -lum00 - 2.0 * lum01 - lum02 + lum20 + 2.0 * lum21 + lum22;
                float edge = sqrt(gx * gx + gy * gy);

                // Soglia e contrasto per contorni neon netti e puliti dei soli muri
                float edgeFactor = smoothstep(_EdgeThreshold, _EdgeThreshold * 2.2 + 0.05, edge);

                // Sfondo solido scuro privo di griglia esterna
                fixed4 col = _BackgroundColor;

                // Aggiunta contorni verde neon brillante
                fixed3 neonColor = _EdgeColor.rgb * _EdgeGlow;
                col.rgb = lerp(col.rgb, neonColor, edgeFactor);

                col.a = _BackgroundColor.a;
                col *= IN.color;
                return col;
            }
            ENDCG
        }
    }
}
