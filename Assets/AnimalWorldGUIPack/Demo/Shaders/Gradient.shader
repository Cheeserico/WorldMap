Shader "Sprites/Gradient"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _ColorTop ("Top color", Color) = (1, 1, 1, 1)
        _ColorBottom ("Bottom color", Color) = (1, 1, 1, 1)
        [MaterialToggle] PixelSnap ("Pixel snap", Float) = 0
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

        Cull Off
        ZWrite Off
        Blend One OneMinusSrcAlpha

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
                half4  _ColorTop;
                half4  _ColorBottom;
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
                OUT.color = lerp(_ColorBottom, _ColorTop, IN.uv.y);

                #ifdef PIXELSNAP_ON
                OUT.positionCS = PixelSnapCS(OUT.positionCS);
                #endif

                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half4 c = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv) * IN.color;
                c.rgb *= c.a;
                return c;
            }
            ENDHLSL
        }
    }
}
