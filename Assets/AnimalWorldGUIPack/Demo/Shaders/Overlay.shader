Shader "Sprites/Overlay"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        [MaterialToggle] PixelSnap ("Pixel snap", Float) = 0
        _Intensity ("Intensity", Range (0, 1)) = 1.0

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
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        ColorMask [_ColorMask]

        Cull Off
        ZWrite Off
        Blend DstColor SrcColor

        Pass
        {
            // The 2D Renderer looks for this tag. If you are on the regular
            // Universal Renderer instead, change it to "UniversalForward".
            Tags { "LightMode" = "Universal2D" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_local _ PIXELSNAP_ON

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float3 positionOS : POSITION;
                float4 color      : COLOR;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half4  color      : COLOR;
                float2 uv         : TEXCOORD0;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half4  _Color;
                float  _Intensity;
            CBUFFER_END

            // URP has no UnityPixelSnap, so this is the equivalent.
            float4 PixelSnapCS(float4 positionCS)
            {
                float2 hpc = _ScreenParams.xy * 0.5;
                float2 pixelPos = round((positionCS.xy / positionCS.w) * hpc);
                positionCS.xy = pixelPos / hpc * positionCS.w;
                return positionCS;
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT = (Varyings)0;

                OUT.positionCS = TransformObjectToHClip(IN.positionOS);
                OUT.uv = IN.uv;
                OUT.color = IN.color * _Color;

                #ifdef PIXELSNAP_ON
                OUT.positionCS = PixelSnapCS(OUT.positionCS);
                #endif

                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);

                half4 final;
                final.rgb = IN.color.rgb * tex.rgb * 2;
                final.a   = IN.color.a * tex.a;

                // 0.5 is the neutral value for the DstColor/SrcColor (2x multiply) blend.
                return lerp(half4(0.5, 0.5, 0.5, 0.5), final, final.a);
            }
            ENDHLSL
        }
    }
}
