Shader "UI/TacticalNeonMap"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _EdgeColor ("Neon Green Edge", Color) = (0.0, 1.0, 0.45, 1.0)
        _BackgroundColor ("Background Color", Color) = (0.005, 0.018, 0.012, 0.98)
        _EdgeThreshold ("Edge Sensitivity", Range(0.01, 0.8)) = 0.08
        _EdgeGlow ("Edge Glow Intensity", Range(1.0, 5.0)) = 2.6
        _BlueprintStrength ("Blueprint Room Visibility", Range(0.0, 1.0)) = 0.35

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
            float _BlueprintStrength;

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

                // Campionamento kernel 3x3 Sobel con saturazione per evitare sovraesposizione HDR
                float3 c00 = saturate(tex2D(_MainTex, uv + float2(-d.x, -d.y)).rgb);
                float3 c01 = saturate(tex2D(_MainTex, uv + float2( 0.0, -d.y)).rgb);
                float3 c02 = saturate(tex2D(_MainTex, uv + float2( d.x, -d.y)).rgb);
                float3 c10 = saturate(tex2D(_MainTex, uv + float2(-d.x,  0.0)).rgb);
                float3 c11 = saturate(tex2D(_MainTex, uv).rgb);
                float3 c12 = saturate(tex2D(_MainTex, uv + float2( d.x,  0.0)).rgb);
                float3 c20 = saturate(tex2D(_MainTex, uv + float2(-d.x,  d.y)).rgb);
                float3 c21 = saturate(tex2D(_MainTex, uv + float2( 0.0,  d.y)).rgb);
                float3 c22 = saturate(tex2D(_MainTex, uv + float2( d.x,  d.y)).rgb);

                // Calcolo gradiente di luminanza
                float lum00 = dot(c00, float3(0.299, 0.587, 0.114));
                float lum01 = dot(c01, float3(0.299, 0.587, 0.114));
                float lum02 = dot(c02, float3(0.299, 0.587, 0.114));
                float lum10 = dot(c10, float3(0.299, 0.587, 0.114));
                float lum11 = dot(c11, float3(0.299, 0.587, 0.114));
                float lum12 = dot(c12, float3(0.299, 0.587, 0.114));
                float lum20 = dot(c20, float3(0.299, 0.587, 0.114));
                float lum21 = dot(c21, float3(0.299, 0.587, 0.114));
                float lum22 = dot(c22, float3(0.299, 0.587, 0.114));

                float gx = -lum00 + lum02 - 2.0 * lum10 + 2.0 * lum12 - lum20 + lum22;
                float gy = -lum00 - 2.0 * lum01 - lum02 + lum20 + 2.0 * lum21 + lum22;
                float edge = saturate(sqrt(gx * gx + gy * gy));

                // Soglia e contrasto per contorni neon netti e puliti dei soli muri
                float edgeFactor = smoothstep(_EdgeThreshold, _EdgeThreshold * 2.2 + 0.03, edge);

                // Base olografica scura delle stanze (il pavimento rimane sempre scuro e leggibile, mai bianco)
                fixed3 blueprintRoom = _BackgroundColor.rgb + saturate(lum11) * fixed3(0.015, 0.06, 0.035) * _BlueprintStrength;

                // Contorni verde neon brillante sui bordi di muri e strutture
                fixed3 neonColor = _EdgeColor.rgb * _EdgeGlow;
                fixed3 finalRgb = lerp(blueprintRoom, neonColor, edgeFactor);

                // Sottile scanline futuristica
                float scanline = sin(uv.y * 500.0) * 0.03 + 0.97;
                finalRgb *= scanline;

                fixed4 col = fixed4(finalRgb, _BackgroundColor.a);
                col *= IN.color;

                return col;
            }
            ENDCG
        }
    }
}
