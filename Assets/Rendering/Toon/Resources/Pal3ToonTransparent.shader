// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

Shader "Pal3/ToonTransparent"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _TintColor ("Tint Color", Color) = (1, 1, 1, 1)
        _ShadowTex ("Baked Shadow", 2D) = "white" {}
        _HasShadowTex ("Has Baked Shadow", Float) = 0
        _LightmapScale ("Baked Light Scale", Float) = 1.666667
        _ReceiveShadows ("Receive Shadows", Float) = 1
        [HideInInspector] _IsCharacter ("Character", Float) = 0
        _Threshold ("Alpha Cutoff", Float) = 1
        _ReceiveLighting ("Receive Lighting", Float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _BlendSrcFactor ("Source Blend Factor", Int) = 5
        [Enum(UnityEngine.Rendering.BlendMode)] _BlendDstFactor ("Destination Blend Factor", Int) = 10
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "Queue" = "Transparent" }

        Pass
        {
            Name "ToonForward"
            Tags { "LightMode" = "UniversalForward" }
            Blend [_BlendSrcFactor] [_BlendDstFactor]
            ZWrite Off

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex ToonVertex
            #pragma fragment ToonFragment
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Fog.hlsl"
            #define PAL3_TOON_TRANSPARENT
            #include "Assets/Rendering/Toon/Pal3ToonForward.hlsl"
            ENDHLSL
        }
    }
}
