// See LICENSE in the project root.

#include "Pal3ToonInput.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

float _Pal3ToonCharacterLighting;
float _Pal3ToonNightFireBoost;
float4 _Pal3ToonSceneLighting;
float4 _Pal3ToonEffectStrengths;

struct ToonVaryings
{
    float4 positionCS : SV_POSITION;
    float2 uv : TEXCOORD0;
    float2 lightmapUV : TEXCOORD1;
    float3 positionWS : TEXCOORD2;
    half3 normalWS : TEXCOORD3;
    float fogFactor : TEXCOORD4;
    UNITY_VERTEX_OUTPUT_STEREO
};

ToonVaryings ToonVertex(ToonAttributes input)
{
    ToonVaryings output;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
    output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
    output.positionCS = TransformWorldToHClip(output.positionWS);
    output.normalWS = TransformObjectToWorldNormal(input.normalOS);
    output.uv = TRANSFORM_TEX(input.uv, _MainTex);
    output.lightmapUV = TRANSFORM_TEX(input.lightmapUV, _ShadowTex);
    output.fogFactor = ComputeFogFactor(output.positionCS.z);
    return output;
}

#if defined(_ADDITIONAL_LIGHTS)
float3 ShadeToonFill(uint lightIndex, float3 positionWS, half3 normalWS)
{
    Light light;
    #if defined(_ADDITIONAL_LIGHT_SHADOWS)
        if (_ReceiveShadows > 0.5)
            light = GetAdditionalLight(lightIndex, positionWS, half4(1, 1, 1, 1));
        else
    #endif
            light = GetAdditionalLight(lightIndex, positionWS);
    half weight = smoothstep(-0.16, 0.54, dot(normalWS, light.direction));
    return light.color * (weight * light.distanceAttenuation * light.shadowAttenuation);
}
#endif

half3 ShadeToon(half3 albedo, half3 baked, float4 positionCS, float3 positionWS, half3 normalWS)
{
    Light key = GetMainLight();
    #if defined(MAIN_LIGHT_CALCULATE_SHADOWS)
        if (_ReceiveShadows > 0.5)
            key.shadowAttenuation = MainLightRealtimeShadow(TransformWorldToShadowCoord(positionWS));
        key.shadowAttenuation = lerp(key.shadowAttenuation, 1.0, GetMainLightShadowFade(positionWS));
    #endif
    half3 keyColor = key.color * key.distanceAttenuation;
    if (max(max(keyColor.r, keyColor.g), keyColor.b) <= 0.001)
        return albedo * baked;

    half diffuse = dot(normalWS, key.direction);
    // Two narrow transitions give three value bands with softened edges.
    half bands = 0.19 * smoothstep(-0.18, -0.06, diffuse) + 0.38 * smoothstep(0.34, 0.46, diffuse);
    half3 viewDir = GetWorldSpaceNormalizeViewDir(positionWS);
    half rim = smoothstep(0.67, 0.87, 1.0 - saturate(dot(normalWS, viewDir)));
    rim *= 0.14 * smoothstep(0.03, 0.45, diffuse);
    half night = _Pal3ToonSceneLighting.x;
    half indoors = _Pal3ToonSceneLighting.y;
    half contrast = _Pal3ToonSceneLighting.z;
    half visibility = lerp(lerp(0.22, 0.10, contrast), 1.0, key.shadowAttenuation);
    half3 ambient = lerp(half3(0.58, 0.59, 0.63), half3(0.29, 0.32, 0.46), night);
    half3 dayAmbient = lerp(half3(0.46, 0.48, 0.53), half3(0.40, 0.39, 0.39), indoors);
    half3 nightAmbient = lerp(half3(0.22, 0.27, 0.41), half3(0.20, 0.235, 0.33), indoors);
    ambient = lerp(ambient, lerp(dayAmbient, nightAmbient, night), contrast);
    UNITY_BRANCH
    if (_IsCharacter > 0.5 && _Pal3ToonCharacterLighting > 0.0)
    {
        // Retain facial readability while allowing the body and cast shadows to have contrast.
        half fillStrength = _Pal3ToonCharacterLighting;
        half3 characterAmbient = lerp(half3(0.72, 0.73, 0.77), half3(0.50, 0.52, 0.64), night);
        ambient = lerp(ambient, characterAmbient, fillStrength);
        half characterBands = 0.17 * smoothstep(-0.39, -0.19, diffuse) + 0.26 * smoothstep(0.26, 0.42, diffuse);
        bands = lerp(bands, characterBands, fillStrength);
        visibility = lerp(visibility, lerp(0.40, 1.0, key.shadowAttenuation), fillStrength);
    }
    half3 direct = keyColor * ((bands + rim) * visibility);

    #if defined(_ADDITIONAL_LIGHTS)
        float3 fill = 0;
        #if USE_CLUSTER_LIGHT_LOOP
            InputData inputData = (InputData)0;
            inputData.positionWS = positionWS;
            inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(positionCS);

            // URP stores additional directional lights outside the punctual light clusters.
            [loop] for (uint lightIndex = 0; lightIndex < min(URP_FP_DIRECTIONAL_LIGHTS_COUNT, MAX_VISIBLE_LIGHTS); ++lightIndex)
            {
                CLUSTER_LIGHT_LOOP_SUBTRACTIVE_LIGHT_CHECK
                fill += ShadeToonFill(lightIndex, positionWS, normalWS);
            }
        #endif
        uint lightCount = GetAdditionalLightsCount();
        LIGHT_LOOP_BEGIN(lightCount)
            fill += ShadeToonFill(lightIndex, positionWS, normalWS);
        LIGHT_LOOP_END
        float3 scaledFill = fill * lerp(0.34, 0.42, night * contrast);
        if (_Pal3ToonNightFireBoost > 0.5)
        {
            // Limit the fill without clipping its red and green channels separately.
            float peak = max(max(scaledFill.r, scaledFill.g), scaledFill.b);
            direct += scaledFill * (0.90 / (0.90 + peak));
        }
        else
        {
            direct += min(scaledFill, 0.45);
        }
    #endif

    half occlusion = 1.0;
    #if !defined(PAL3_TOON_TRANSPARENT) && !defined(PAL3_TOON_WATER)
    UNITY_BRANCH
    if (_Pal3ToonEffectStrengths.x > 0.0)
    {
        // The per-camera switch also controls the SSAO pass. Sampling directly avoids
        // depending on URP retaining an SSAO-off shader variant for runtime toggles.
        half sampledAO = saturate(SampleAmbientOcclusion(GetNormalizedScreenSpaceUV(positionCS)));
        half strength = _Pal3ToonEffectStrengths.x;
        occlusion = lerp(1.0, sampledAO, min(strength, 1.0));
        occlusion = lerp(occlusion, sampledAO * sampledAO, saturate(strength - 1.0));
        // Prevent strong AO from obscuring small facial features.
        occlusion = lerp(occlusion, lerp(1.0, occlusion, 0.70), _IsCharacter * _Pal3ToonCharacterLighting);
    }
    #endif

    half3 bakedVisibility = lerp(1.0, saturate(baked), _Pal3ToonEffectStrengths.y);
    // Static occlusion shapes ambient lighting; direct candle light can still brighten dark corners.
    half3 lighting = ambient * bakedVisibility * occlusion;
    lighting += direct * lerp(1.0, bakedVisibility, 0.55) * lerp(1.0, occlusion, 0.15);
    return albedo * lighting;
}

half4 ToonFragment(ToonVaryings input) : SV_Target
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
    half4 color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
    #if defined(PAL3_TOON_CUTOUT)
        clip(color.a - _Threshold);
    #elif defined(PAL3_TOON_TRANSPARENT)
        if (color.a >= _Threshold || (color.a <= 0.0 && _Threshold <= 1.0))
            discard;
    #endif

    half3 baked = 1.0;
    if (_HasShadowTex > 0.5)
        baked = SAMPLE_TEXTURE2D(_ShadowTex, sampler_ShadowTex, input.lightmapUV).rgb * _LightmapScale;

    color *= _TintColor;
    #if defined(PAL3_TOON_WATER)
        // The first frame defines opacity for the full water animation.
        color.a = _Alpha;
    #endif
    #if defined(PAL3_TOON_TRANSPARENT)
        if (_ReceiveLighting < 0.5)
            color.rgb *= baked;
        else
    #endif
        color.rgb = ShadeToon(color.rgb, baked, input.positionCS, input.positionWS, SafeNormalize(input.normalWS));

    color.rgb = MixFog(color.rgb, input.fogFactor);
    return color;
}
