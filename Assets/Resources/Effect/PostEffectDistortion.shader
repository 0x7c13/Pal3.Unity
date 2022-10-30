Shader "Pal3/PostEffectDistortion"
{
    Properties
    {
        _Blend("Blend Factor", float) = 10
    }
    
  HLSLINCLUDE
// StdLib.hlsl holds pre-configured vertex shaders (VertDefault), varying structs (VaryingsDefault), and most of the data you need to write common effects.
      #include "Packages/com.unity.postprocessing/PostProcessing/Shaders/StdLib.hlsl"
      TEXTURE2D_SAMPLER2D(_MainTex, sampler_MainTex);
// Lerp the pixel color with the luminance using the _Blend uniform.
      float _Blend;
      float4 Frag(VaryingsDefault i) : SV_Target
      {
          float4 color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.texcoord);
// Compute the luminance for the current pixel
          float luminance = dot(color.rgb, float3(0.2126729, 0.7151522, 0.0721750));
          color.rgb = lerp(color.rgb, luminance.xxx, _Blend.xxx);
// Return the result
          return color;
      }
  ENDHLSL
  SubShader
  {
      Cull Off ZWrite Off ZTest Always
      Pass
      {
          HLSLPROGRAM
              #pragma vertex VertDefault
              #pragma fragment Frag
          ENDHLSL
      }
  }
}