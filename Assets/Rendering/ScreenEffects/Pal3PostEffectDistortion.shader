// ------------------------------------------------------------------------
//  Copyright (c) 2021-2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

/*
    reference: https://www.shadertoy.com/view/MsKXDh
*/

Shader "Pal3/PostEffectDistortion"
{
    Properties
    {
        _TimeScale ("Time Scale",float) = 4.0
        _XFactor ("X Factor",float) = 15.0
        _YFactor("Y Factor",float) = 10.0
    }

    HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

        float _TimeScale;
        float _XFactor;
        float _YFactor;

        float4 Frag(Varyings i) : SV_Target
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
            float2 uv = i.texcoord;
            const float time = _Time.y;

            uv.x += sin(uv.y * _XFactor + time * _TimeScale) / 400.0;
            uv.y += cos(uv.x * _YFactor + time * _TimeScale) / 450.0;

            uv.x += sin((uv.y+uv.x) * _XFactor + time * _TimeScale) / (180.0 + (_TimeScale * sin(time)));
            uv.y += cos((uv.y+uv.x) * _YFactor + time * _TimeScale) / (200.0 + (_TimeScale * sin(time)));

            float4 col = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp,uv);
            return col;
        }
    ENDHLSL

    SubShader
    {
      Tags { "RenderPipeline" = "UniversalPipeline" }
      Cull Off ZWrite Off ZTest Always
      Pass
      {
          HLSLPROGRAM
              #pragma vertex Vert
              #pragma fragment Frag
          ENDHLSL
      }
    }
}
