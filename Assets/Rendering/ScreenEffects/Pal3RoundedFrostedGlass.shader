// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

// Based on cician's shader from https://forum.unity3d.com/threads/simple-optimized-blur-shader.185327/#post-1267642
// Based on RoundedCorners.shader from https://github.com/kirevdokimov/Unity-UI-Rounded-Corners
// with some modifications

Shader "Pal3/RoundedFrostedGlass"
{
    Properties
    {
        _BlurAmount ("Blur", Range(0, 30)) = 1
        _Transparency ("Transparency", Range(0, 1)) = 1
        [HideInInspector] _MainTex ("Masking Texture", 2D) = "white" {}
        _AdditiveColor ("Additive Tint color", Color) = (0, 0, 0, 0)
        _MultiplyColor ("Multiply Tint color", Color) = (1, 1, 1, 1)
        _WidthHeightRadius ("WidthHeightRadius", Vector) = (0, 0, 0, 0)
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "Queue" = "Transparent" "RenderType" = "Transparent" }
        Cull Off
        ZTest [unity_GUIZTestMode]
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_Pal3SceneColor);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _WidthHeightRadius;
                half4 _AdditiveColor;
                half4 _MultiplyColor;
                float _BlurAmount;
                float _Transparency;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 screenPosition : TEXCOORD1;
                half alpha : TEXCOORD2;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.screenPosition = output.positionCS;
                output.alpha = input.color.a;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 size = _WidthHeightRadius.xy;
                float radius = _WidthHeightRadius.z * 0.5;
                float2 edge = abs((input.uv - 0.5) * size) - (size * 0.5 - radius);
                float distanceToRect = length(max(edge, 0)) + min(max(edge.x, edge.y), 0) - radius;
                float edgeWidth = max(fwidth(distanceToRect), 0.001);
                float roundedAlpha = 1.0 - smoothstep(-edgeWidth, edgeWidth, distanceToRect);
                half mask = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv).a;

                // The URP color copy uses Unity's bottom-left texture orientation.
                float2 uv = input.screenPosition.xy / input.screenPosition.w * 0.5 + 0.5;
                float2 pixelSize = abs(float2(ddx(uv.x), ddy(uv.y)));
                float2 delta = pixelSize * (_BlurAmount * 1.2);
                half3 blurred = 0;
                [unroll] for (int y = -1; y <= 1; y++)
                {
                    [unroll] for (int x = -1; x <= 1; x++)
                    {
                        // Bilinear taps approximate a separable five-tap Gaussian.
                        half weightX = x == 0 ? 0.375 : 0.3125;
                        half weightY = y == 0 ? 0.375 : 0.3125;
                        blurred += SAMPLE_TEXTURE2D(_Pal3SceneColor, sampler_LinearClamp,
                            uv + float2(x, y) * delta).rgb * (weightX * weightY);
                    }
                }

                return half4(blurred * _MultiplyColor.rgb + _AdditiveColor.rgb,
                    mask * roundedAlpha * _Transparency * input.alpha);
            }
            ENDHLSL
        }
    }
}
