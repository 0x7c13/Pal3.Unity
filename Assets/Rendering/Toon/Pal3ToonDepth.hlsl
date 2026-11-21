// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

#include "Pal3ToonInput.hlsl"

struct ToonDepthVaryings
{
    float4 positionCS : SV_POSITION;
    #if defined(PAL3_TOON_CUTOUT)
        float2 uv : TEXCOORD0;
        float2 lightmapUV : TEXCOORD2;
        float4 paintedVertexColor : TEXCOORD3;
    #endif
    #if defined(PAL3_TOON_DEPTH_NORMALS)
        half3 normalWS : TEXCOORD1;
    #endif
    UNITY_VERTEX_OUTPUT_STEREO
};

ToonDepthVaryings ToonDepthVertex(ToonAttributes input)
{
    ToonDepthVaryings output;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
    output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
    #if defined(PAL3_TOON_DEPTH_NORMALS)
        output.normalWS = TransformObjectToWorldNormal(input.normalOS);
    #endif
    #if defined(PAL3_TOON_CUTOUT)
        output.uv = TRANSFORM_TEX(input.uv, _MainTex);
        output.lightmapUV = TRANSFORM_TEX(input.lightmapUV, _ShadowTex);
        output.paintedVertexColor = ToonPaintedVertexColor(input.color);
    #endif
    return output;
}

#if defined(PAL3_TOON_DEPTH_NORMALS)
half4 ToonDepthFragment(ToonDepthVaryings input) : SV_Target
#else
half ToonDepthFragment(ToonDepthVaryings input) : SV_Target
#endif
{
    #if defined(PAL3_TOON_CUTOUT)
        half4 color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
        half alpha = color.a;
        if (_UnlitBackground > 0.5)
        {
            half4 lightmap = _HasShadowTex > 0.5 ? SAMPLE_TEXTURE2D(_ShadowTex, sampler_ShadowTex, input.lightmapUV) : 1.0;
            alpha = ToonPaintedTextureStages(color, lightmap, input.paintedVertexColor).a;
        }
        clip(alpha - _Threshold);
    #endif
    #if defined(PAL3_TOON_DEPTH_NORMALS)
        float3 normalWS = SafeNormalize(input.normalWS);
        #if defined(_GBUFFER_NORMALS_OCT)
            float2 oct = saturate(PackNormalOctQuadEncode(normalWS) * 0.5 + 0.5);
            return half4(PackFloat2To888(oct), 0);
        #else
            return half4(normalWS, 0);
        #endif
    #else
        return input.positionCS.z;
    #endif
}
