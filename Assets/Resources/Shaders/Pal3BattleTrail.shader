// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

Shader "Hidden/Pal3/BattleTrail"
{
    Properties
    {
        _MainTex ("History", 2D) = "black" {}
        _Diffuse ("Original diffuse", Vector) = (1, 1, 1, 1)
    }
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }
        Pass
        {
            Blend SrcAlpha One
            ZTest Always
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_TexelSize;
                float4 _Diffuse;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                #if UNITY_UV_STARTS_AT_TOP
                if (_MainTex_TexelSize.y < 0)
                    output.uv.y = 1 - output.uv.y;
                #endif
                output.uv.y *= .75;
                return output;
            }

            float4 Frag(Varyings input) : SV_Target
            {
                return float4(SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv).rgb * _Diffuse.rgb, _Diffuse.a);
            }
            ENDHLSL
        }
    }
}
