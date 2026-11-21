// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

#ifndef PAL3_TOON_INPUT_INCLUDED
#define PAL3_TOON_INPUT_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

TEXTURE2D(_MainTex);
SAMPLER(sampler_MainTex);
TEXTURE2D(_ShadowTex);
SAMPLER(sampler_ShadowTex);

float _GbVertexColor;

CBUFFER_START(UnityPerMaterial)
    float4 _MainTex_ST;
    float4 _ShadowTex_ST;
    half4 _TintColor;
    half _BattleSelection;
    float _Threshold;
    float _HasShadowTex;
    float _LightmapScale;
    float _ReceiveLighting;
    float _ReceiveShadows;
    float _IsCharacter;
    float _UnlitBackground;
    float _BackgroundBrightness;
    float _SourceDiffuseAlpha;
    #if defined(PAL3_TOON_WATER)
        float _Alpha;
    #endif
CBUFFER_END

struct ToonAttributes
{
    float4 positionOS : POSITION;
    float3 normalOS : NORMAL;
    float4 color : COLOR;
    float2 uv : TEXCOORD0;
    float2 lightmapUV : TEXCOORD1;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

float4 ToonPaintedVertexColor(float4 vertexColor)
{
    float4 color = _GbVertexColor > 0.5 ? vertexColor : float4(1, 1, 1, _SourceDiffuseAlpha);
    color.a *= _TintColor.a;
    return color;
}

half4 ToonPaintedTextureStages(half4 mainColor, half4 lightmap, float4 vertexColor)
{
    // Original static POL stages. Painted surfaces remain independent of scene lights.
    vertexColor = saturate(vertexColor);
    if (_HasShadowTex > 0.5)
        return saturate(half4(lightmap.rgb * mainColor.rgb * 2,
            lightmap.a * mainColor.a * vertexColor.a * 2));
    return saturate(mainColor * vertexColor);
}

#endif
