// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

#include "Pal3ToonInput.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

float3 _LightDirection;
float3 _LightPosition;

struct ToonShadowVaryings
{
    float4 positionCS : SV_POSITION;
    #if defined(PAL3_TOON_CUTOUT)
        float2 uv : TEXCOORD0;
    #endif
};

ToonShadowVaryings ToonShadowVertex(ToonAttributes input)
{
    ToonShadowVaryings output;
    UNITY_SETUP_INSTANCE_ID(input);
    float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
    float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
    #if defined(_CASTING_PUNCTUAL_LIGHT_SHADOW)
        float3 lightDirectionWS = normalize(_LightPosition - positionWS);
    #else
        float3 lightDirectionWS = _LightDirection;
    #endif
    output.positionCS = ApplyShadowClamping(TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirectionWS)));
    #if defined(PAL3_TOON_CUTOUT)
        output.uv = TRANSFORM_TEX(input.uv, _MainTex);
    #endif
    return output;
}

half4 ToonShadowFragment(ToonShadowVaryings input) : SV_Target
{
    #if defined(PAL3_TOON_CUTOUT)
        clip(SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv).a - _Threshold);
    #endif
    return 0;
}
