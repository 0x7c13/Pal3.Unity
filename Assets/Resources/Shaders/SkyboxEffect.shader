// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

Shader "Pal3/SkyboxEffect"
{
    Properties { _MainTex ("Sequence frame", 2D) = "black" {} }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent-10" "RenderType"="Transparent" }
        Pass
        {
            Blend SrcAlpha One
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };
            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                // Sky effects must remain behind all scene geometry, including
                // mountains farther away than the effect cube's actual mesh.
                #if UNITY_REVERSED_Z
                output.positionCS.z = 0.0;
                #else
                output.positionCS.z = output.positionCS.w;
                #endif
                output.uv = input.uv;
                return output;
            }
            half4 frag(Varyings input) : SV_Target { return SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv); }
            ENDHLSL
        }
    }
}
