Shader "Unlit/EffectViewer/GenericGrid"
{
    Properties
    {
        [Header(Checkerboard Background)]
        [Toggle] _EnableCheckerboard ("Enable Checkerboard", Float) = 0
        _CheckerboardSize ("Checkerboard Size", Float) = 10.0
        _CheckerColor1 ("Checker Color 1 (Dark)", Color) = (0.3, 0.3, 0.3, 1)
        _CheckerColor2 ("Checker Color 2 (Light)", Color) = (0.5, 0.5, 0.5, 1)
        _CheckerAlpha ("Checkerboard Alpha", Range(0, 1)) = 1.0

        [Header(Main Grid)]
        [Toggle] _EnableMainGrid ("Enable Main Grid", Float) = 1
        _GridSize ("Grid Size", Float) = 1.0
        _GridWidth ("Grid Line Width", Range(0.001, 0.2)) = 0.05
        _GridColor ("Grid Color", Color) = (1, 1, 1, 1)
        _GridAlpha ("Grid Alpha", Range(0, 1)) = 0.8

        [Header(Sub Grid)]
        [Toggle] _EnableSubGrid ("Enable Sub Grid", Float) = 1
        _SubGridSize ("Sub Grid Size", Float) = 10.0
        _SubGridWidth ("Sub Grid Line Width", Range(0.001, 0.2)) = 0.03
        _SubGridColor ("Sub Grid Color", Color) = (1, 1, 1, 0.5)
        _SubGridAlpha ("Sub Grid Alpha", Range(0, 1)) = 0.5
    }

    SubShader
    {
        Tags
        {
            "RenderType"="Transparent"
            "Queue"="Transparent"
            "IgnoreProjector"="True"
        }
        LOD 100

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
            };

            float _EnableCheckerboard;
            float _CheckerboardSize;
            float4 _CheckerColor1;
            float4 _CheckerColor2;
            float _CheckerAlpha;

            float _EnableMainGrid;
            float _GridSize;
            float _GridWidth;
            float4 _GridColor;
            float _GridAlpha;

            float _EnableSubGrid;
            float _SubGridSize;
            float _SubGridWidth;
            float4 _SubGridColor;
            float _SubGridAlpha;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                UNITY_TRANSFER_FOG(o, o.vertex);
                return o;
            }

            // Generate grid pattern
            float grid(float2 uv, float size, float width)
            {
                // Scale UV coordinates by grid size
                float2 scaledUV = uv * size;

                // Calculate grid lines for each axis
                float2 gridCoord = abs(frac(scaledUV - 0.5) - 0.5) / fwidth(scaledUV);
                float dist = min(gridCoord.x, gridCoord.y);

                // Apply line width with anti-aliasing
                return 1.0 - min(dist / width, 1.0);
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Initialize with transparent color
                float4 finalColor = float4(0, 0, 0, 0);

                // Generate checkerboard background if enabled
                if (_EnableCheckerboard > 0.5)
                {
                    float2 scaledUV = i.uv * _CheckerboardSize;
                    float2 checker = floor(scaledUV);
                    float pattern = fmod(checker.x + checker.y, 2.0);
                    float4 checkerColor = lerp(_CheckerColor1, _CheckerColor2, pattern);
                    checkerColor.a *= _CheckerAlpha;
                    finalColor = checkerColor;
                }

                // Generate and blend sub grid if enabled
                if (_EnableSubGrid > 0.5)
                {
                    float subGridPattern = grid(i.uv, _SubGridSize, _SubGridWidth * 100.0);
                    float4 subGridColor = _SubGridColor;
                    subGridColor.a *= subGridPattern * _SubGridAlpha;

                    finalColor.rgb = lerp(finalColor.rgb, subGridColor.rgb, subGridColor.a);
                    finalColor.a = max(finalColor.a, subGridColor.a);
                }

                // Generate and blend main grid if enabled
                if (_EnableMainGrid > 0.5)
                {
                    float mainGridPattern = grid(i.uv, _GridSize, _GridWidth * 100.0);
                    float4 mainGridColor = _GridColor;
                    mainGridColor.a *= mainGridPattern * _GridAlpha;

                    finalColor.rgb = lerp(finalColor.rgb, mainGridColor.rgb, mainGridColor.a);
                    finalColor.a = max(finalColor.a, mainGridColor.a);
                }

                // Apply fog
                UNITY_APPLY_FOG(i.fogCoord, finalColor);

                return finalColor;
            }
            ENDCG
        }
    }

    Fallback "Hidden/InternalErrorShader"
}
