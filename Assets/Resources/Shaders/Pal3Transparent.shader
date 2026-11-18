// ------------------------------------------------------------------------
//  Copyright (c) 2021-2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

Shader "Pal3/Transparent"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        [HideInInspector] _GbNativeLighting ("Original battle lighting", Float) = 0
        [HideInInspector] _GbMaterialDiffuse ("Original material diffuse", Vector) = (1, 1, 1, 1)
        [HideInInspector] _GbMaterialAmbient ("Original material ambient", Vector) = (1, 1, 1, 1)
        [HideInInspector] _BattleSelection ("Battle selection luminance", Float) = -1
        _TintColor ("Tint color", Color) = (1.0, 1.0, 1.0, 1.0)
        _Threshold ("Transparent Threshold", Range(0, 1)) = 1.0

        _HasShadowTex ("Has Shadow Texture", Range(0, 1)) = 0.0
        _ShadowTex ("Shadow Texture", 2D) = "white" {}
        _LightmapScale("Lightmap Scale", Float) = 1.666667

        [Enum(UnityEngine.Rendering.BlendMode)]
        _BlendSrcFactor("Source Blend Factor", int) = 5    // BlendMode.SrcAlpha as Default

        [Enum(UnityEngine.Rendering.BlendMode)]
        _BlendDstFactor("Dest Blend Factor", int) = 10     // BlendMode.OneMinusSrcAlpha as Default
    }
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "Queue" = "Transparent" }

        // Pass 2 ,transparent part
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
                float3 normal : NORMAL;
                float4 color : COLOR;
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
                float4 nativeColor : TEXCOORD2;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_ShadowTex);
            SAMPLER(sampler_ShadowTex);

            CBUFFER_START(UnityPerMaterial)
                float _GbNativeLighting;
                float4 _GbMaterialDiffuse;
                float4 _GbMaterialAmbient;
                float4 _MainTex_ST;
                float _Threshold;
                float _HasShadowTex;
                float4 _ShadowTex_ST;
                float _LightmapScale;
                float4 _TintColor;
                float _BattleSelection;
            CBUFFER_END

            #include "GameBoxNativeLighting.hlsl"

            v2f vert(appdata_t v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.vertex = TransformObjectToHClip(v.vertex.xyz);
                o.nativeColor = GameBoxVertexColor(v.vertex.xyz, v.normal, v.color);
                o.texcoord0 = TRANSFORM_TEX(v.texcoord0, _MainTex);
                o.texcoord1 = TRANSFORM_TEX(v.texcoord1, _ShadowTex);
                o.fogFactor = ComputeFogFactor(o.vertex.z);
                return o;
            }

            half4 frag(v2f i) : SV_Target
            {
                half4 color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.texcoord0);
                half3 selectionTexture = color.rgb;
                if (_GbNativeLighting > 0.5)
                {
                    half4 lightmap = _HasShadowTex > 0.5 ? SAMPLE_TEXTURE2D(_ShadowTex, sampler_ShadowTex, i.texcoord1) : 1;
                    color = GameBoxTextureStages(color, lightmap, i.nativeColor, _HasShadowTex);
                }
                else if (_GbVertexColor > 0.5)
                    color *= i.nativeColor;
                if (color.a >= _Threshold || (color.a <= 0.0 && _Threshold <= 1.0))
                {
                    discard;
                }

                if(_HasShadowTex > 0.5f && _GbNativeLighting < 0.5)
                {
                    color.rgb *= SAMPLE_TEXTURE2D(_ShadowTex, sampler_ShadowTex, i.texcoord1).rgb * _LightmapScale;
                }
                if (_GbNativeLighting > 0.5)
                    color.rgb *= _TintColor.rgb;
                else
                    color *= _TintColor;
                if (_BattleSelection >= 0)
                    color.rgb = selectionTexture * _BattleSelection;
                color.rgb = MixFog(color.rgb, i.fogFactor);
                return color;
            }
            ENDHLSL
        }
    }
}
