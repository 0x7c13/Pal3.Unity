// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

Shader "Pal3/ToonCutout"
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
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }

        Pass
        {
            Name "ToonForward"
            Tags { "LightMode" = "UniversalForward" }
            ZWrite On

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
            #define PAL3_TOON_CUTOUT
            #include "Assets/Rendering/Toon/Pal3ToonForward.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On
            ColorMask R

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex ToonDepthVertex
            #pragma fragment ToonDepthFragment
            #define PAL3_TOON_CUTOUT
            #include "Assets/Rendering/Toon/Pal3ToonDepth.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }
            ZWrite On

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex ToonDepthVertex
            #pragma fragment ToonDepthFragment
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
            #define PAL3_TOON_DEPTH_NORMALS
            #define PAL3_TOON_CUTOUT
            #include "Assets/Rendering/Toon/Pal3ToonDepth.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On
            ZTest LEqual
            ColorMask 0

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex ToonShadowVertex
            #pragma fragment ToonShadowFragment
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #define PAL3_TOON_CUTOUT
            #include "Assets/Rendering/Toon/Pal3ToonShadow.hlsl"
            ENDHLSL
        }
    }
}
