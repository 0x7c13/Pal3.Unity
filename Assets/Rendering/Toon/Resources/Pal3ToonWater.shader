// See LICENSE in the project root.

Shader "Pal3/ToonWater"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _ShadowTex ("Baked Shadow", 2D) = "white" {}
        _HasShadowTex ("Has Baked Shadow", Float) = 0
        _LightmapScale ("Baked Light Scale", Float) = 1.428571
        _Alpha ("Opacity", Range(0, 1)) = 0.5
        _ReceiveShadows ("Receive Shadows", Float) = 1
        [HideInInspector] _TintColor ("Tint Color", Color) = (1, 1, 1, 1)
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "Queue" = "Transparent-1" }

        Pass
        {
            Name "ToonForward"
            Tags { "LightMode" = "UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
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
            #define PAL3_TOON_WATER
            #include "Assets/Rendering/Toon/Pal3ToonForward.hlsl"
            ENDHLSL
        }
    }
}
