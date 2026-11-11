// See LICENSE in the project root.

#ifndef PAL3_TOON_INPUT_INCLUDED
#define PAL3_TOON_INPUT_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

TEXTURE2D(_MainTex);
SAMPLER(sampler_MainTex);
TEXTURE2D(_ShadowTex);
SAMPLER(sampler_ShadowTex);

CBUFFER_START(UnityPerMaterial)
    float4 _MainTex_ST;
    float4 _ShadowTex_ST;
    half4 _TintColor;
    float _Threshold;
    float _HasShadowTex;
    float _LightmapScale;
    float _ReceiveLighting;
    float _ReceiveShadows;
    float _IsCharacter;
CBUFFER_END

struct ToonAttributes
{
    float4 positionOS : POSITION;
    float3 normalOS : NORMAL;
    float2 uv : TEXCOORD0;
    float2 lightmapUV : TEXCOORD1;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

#endif
