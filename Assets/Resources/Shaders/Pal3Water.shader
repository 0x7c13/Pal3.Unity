// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2025, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

Shader "Pal3/Water"
{
    Properties
    {
        // main texture
        _MainTex ("Texture", 2D) = "white" {}

        // alpha value
        _Alpha ("Alpha",Range(0, 1)) = 0.5

        _LightmapScale("Lightmap Scale", Float) = 1.428571

        // pre baked shadow texture
        _ShadowTex ("Shadow texture", 2D) = "white" {}
        _HasShadowTex ("Has Shadow Texture", Range(0, 1)) = 0.0

        [Enum(UnityEngine.Rendering.BlendMode)]
        _BlendSrcFactor("Source Blend Factor", int) = 5    // BlendMode.SrcAlpha as Default

        [Enum(UnityEngine.Rendering.BlendMode)]
        _BlendDstFactor("Dest Blend Factor", int) = 10     // BlendMode.OneMinusSrcAlpha as Default
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "Queue" = "Transparent-1" } // -1 to fix sorting order issue with semi-transparent trees etc.

        Pass
        {
            Tags { "LightMode" = "SRPDefaultUnlit" }
            Blend [_BlendSrcFactor] [_BlendDstFactor]
            ZWrite Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Fog.hlsl"

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float2 texcoord0 : TEXCOORD0;
                float2 texcoord1 : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 texcoord0 : TEXCOORD0;
                float2 texcoord1 : TEXCOORD1;
                float fogFactor : TEXCOORD7;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_ShadowTex);
            SAMPLER(sampler_ShadowTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float _Alpha;
                float _LightmapScale;
                float _HasShadowTex;
                float4 _ShadowTex_ST;
            CBUFFER_END

            v2f vert(appdata_t v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.vertex = TransformObjectToHClip(v.vertex.xyz);
                o.texcoord0 = TRANSFORM_TEX(v.texcoord0, _MainTex);
                o.texcoord1 = TRANSFORM_TEX(v.texcoord1, _ShadowTex);
                o.fogFactor = ComputeFogFactor(o.vertex.z);
                return o;
            }

            half4 frag(v2f i) : SV_Target
            {
                half4 mixedColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.texcoord0);
                if (_HasShadowTex > 0.5)
                {
                    mixedColor.rgb *= SAMPLE_TEXTURE2D(_ShadowTex, sampler_ShadowTex, i.texcoord1).rgb;
                }
                mixedColor.rgb *= _LightmapScale;
                mixedColor.a = _Alpha;
                mixedColor.rgb = MixFog(mixedColor.rgb, i.fogFactor);
                return mixedColor;
            }
            ENDHLSL
        }
    }
}