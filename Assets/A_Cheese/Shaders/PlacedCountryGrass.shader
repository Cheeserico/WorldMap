Shader "UI/Placed Country Grass"
{
    Properties
    {
        [PerRendererData]
        _MainTex ("Sprite Texture", 2D) = "white" {}

        _Color ("Image Color", Color) = (1,1,1,1)

        [Header(Grass Ground)]
        _SoilColor ("Soil Color", Color) =
            (0.90, 0.55, 0.24, 1)

        _GrassColor ("Grass Color", Color) =
            (0.12, 0.42, 0.20, 1)

        _LightGrassColor ("Light Grass Color", Color) =
            (0.35, 0.65, 0.24, 1)

        _PatternSize ("Pattern Size", Range(8, 80)) = 24
        _GrassDensity ("Grass Density", Range(0, 1)) = 0.55
        _GrassWidth ("Grass Width", Range(0.01, 0.15)) = 0.045
        _GrassHeight ("Grass Height", Range(0.08, 0.45)) = 0.24

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255

        _ColorMask ("Color Mask", Float) = 15

        [Toggle(UNITY_UI_ALPHACLIP)]
        _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
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

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                float2 localPosition : TEXCOORD2;
            };

            sampler2D _MainTex;

            fixed4 _Color;
            fixed4 _SoilColor;
            fixed4 _GrassColor;
            fixed4 _LightGrassColor;

            float _PatternSize;
            float _GrassDensity;
            float _GrassWidth;
            float _GrassHeight;

            float4 _ClipRect;

            float Hash21(float2 value)
            {
                value =
                    frac(
                        value *
                        float2(
                            123.34,
                            456.21
                        )
                    );

                value +=
                    dot(
                        value,
                        value + 45.32
                    );

                return frac(
                    value.x *
                    value.y
                );
            }

float SegmentDistance(
    float2 samplePosition,
    float2 lineStart,
    float2 lineEnd
)
{
    float2 positionOffset =
        samplePosition -
        lineStart;

    float2 lineDirection =
        lineEnd -
        lineStart;

    float denominator =
        max(
            dot(
                lineDirection,
                lineDirection
            ),
            0.0001
        );

    float amount =
        saturate(
            dot(
                positionOffset,
                lineDirection
            ) /
            denominator
        );

    return length(
        positionOffset -
        lineDirection * amount
    );
}
            v2f vert(appdata_t input)
            {
                v2f output;

                output.worldPosition =
                    input.vertex;

                output.localPosition =
                    input.vertex.xy;

                output.vertex =
                    UnityObjectToClipPos(
                        input.vertex
                    );

                output.texcoord =
                    input.texcoord;

                output.color =
                    input.color * _Color;

                return output;
            }

            fixed4 frag(v2f input) : SV_Target
            {
                fixed4 sprite =
                    tex2D(
                        _MainTex,
                        input.texcoord
                    );

// 国画像のUV座標を使うことで、
// 地図を動かしても草模様が国へ追従する
float patternCount =
    max(
        _PatternSize,
        1.0
    );

float2 patternPosition =
    input.texcoord *
    patternCount;
                float2 cell =
                    floor(patternPosition);

                float2 cellPosition =
                    frac(patternPosition) -
                    0.5;

                float randomValue =
                    Hash21(cell);

                float isActive =
                    step(
                        1.0 - _GrassDensity,
                        randomValue
                    );

float horizontalOffset =
    (
        Hash21(
            cell + 7.31
        ) -
        0.5
    ) * 0.65;

float verticalOffset =
    (
        Hash21(
            cell + 19.83
        ) -
        0.5
    ) * 0.55;

float2 basePosition =
    float2(
        horizontalOffset,
        verticalOffset
    );

                float bladeHeight =
                    _GrassHeight *
                    (
                        0.75 +
                        Hash21(
                            cell + 13.7
                        ) *
                        0.5
                    );

                float2 centerTip =
                    basePosition +
                    float2(
                        0.0,
                        bladeHeight
                    );

                float2 leftTip =
                    basePosition +
                    float2(
                        -bladeHeight * 0.55,
                        bladeHeight * 0.8
                    );

                float2 rightTip =
                    basePosition +
                    float2(
                        bladeHeight * 0.55,
                        bladeHeight * 0.8
                    );

                float centerBlade =
                    1.0 -
                    smoothstep(
                        _GrassWidth,
                        _GrassWidth + 0.025,
                        SegmentDistance(
                            cellPosition,
                            basePosition,
                            centerTip
                        )
                    );

                float leftBlade =
                    1.0 -
                    smoothstep(
                        _GrassWidth,
                        _GrassWidth + 0.025,
                        SegmentDistance(
                            cellPosition,
                            basePosition,
                            leftTip
                        )
                    );

                float rightBlade =
                    1.0 -
                    smoothstep(
                        _GrassWidth,
                        _GrassWidth + 0.025,
                        SegmentDistance(
                            cellPosition,
                            basePosition,
                            rightTip
                        )
                    );

                float grassMask =
                    saturate(
                        max(
                            centerBlade,
                            max(
                                leftBlade,
                                rightBlade
                            )
                        ) *
                        isActive
                    );

                float lightGrassAmount =
                    step(
                        0.65,
                        Hash21(
                            cell + 28.4
                        )
                    );

                fixed3 selectedGrassColor =
                    lerp(
                        _GrassColor.rgb,
                        _LightGrassColor.rgb,
                        lightGrassAmount
                    );

                fixed3 surfaceColor =
                    lerp(
                        _SoilColor.rgb,
                        selectedGrassColor,
                        grassMask
                    );

                // ImageのColorも掛ける。
                // 正解時に黒から白へ変化させると、
                // 草地Materialが徐々に現れる。
                surfaceColor *=
                    input.color.rgb;

                fixed4 finalColor =
                    fixed4(
                        surfaceColor,
                        sprite.a *
                        input.color.a
                    );

                #ifdef UNITY_UI_CLIP_RECT
                finalColor.a *=
                    UnityGet2DClipping(
                        input.worldPosition.xy,
                        _ClipRect
                    );
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip(
                    finalColor.a -
                    0.001
                );
                #endif

                return finalColor;
            }

            ENDCG
        }
    }
}