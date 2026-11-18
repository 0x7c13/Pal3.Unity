// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

#ifndef PAL3_GAMEBOX_NATIVE_LIGHTING_INCLUDED
#define PAL3_GAMEBOX_NATIVE_LIGHTING_INCLUDED

float _GbVertexUnits;
float _GbVertexColor;
float _GbLightCount;
float4 _GbLightPosition0;
float4 _GbLightPosition1;
float4 _GbLightDiffuse0;
float4 _GbLightDiffuse1;
float4 _GbLightAmbient0;
float4 _GbLightAmbient1;
float4 _GbLightAttenuation0;
float4 _GbLightAttenuation1;
float4x4 _GbSourceFromRenderer;

float3 GameBoxVertexLight(float3 position, float3 normal, float3 lightPosition,
    float3 diffuse, float3 ambient, float3 attenuation)
{
    float3 delta = lightPosition - position;
    float distanceSquared = dot(delta, delta);
    float distance = sqrt(distanceSquared);
    float3 direction = delta / max(distance, 0.000001);
    float strength = rcp(max(dot(attenuation, float3(1, distance, distanceSquared)), 0.000001));
    return strength * max(dot(normal, direction), 0) * _GbMaterialDiffuse.rgb * diffuse +
        _GbMaterialAmbient.rgb * (ambient + 0.2);
}

float4 GameBoxVertexColor(float3 position, float3 normal, float4 vertexColor)
{
    if (_GbVertexColor > 0.5)
        return float4(vertexColor.rgb, vertexColor.a * (_GbNativeLighting > 0.5 ? _TintColor.a : 1));
    if (_GbNativeLighting < 0.5)
        return 1;
    float3 color = _GbMaterialAmbient.rgb * 0.2;
    // CVD geometry bakes its animated node matrix into vertex positions.
    // Undo that matrix for the original node-local light/normal calculation.
    position = mul(_GbSourceFromRenderer, float4(position, 1)).xyz * _GbVertexUnits;
    if (_GbLightCount > 0.5)
        color = GameBoxVertexLight(position, normal, _GbLightPosition0.xyz,
            _GbLightDiffuse0.rgb, _GbLightAmbient0.rgb, _GbLightAttenuation0.xyz);
    if (_GbLightCount > 1.5)
        color += GameBoxVertexLight(position, normal, _GbLightPosition1.xyz,
            _GbLightDiffuse1.rgb, _GbLightAmbient1.rgb, _GbLightAttenuation1.xyz);
    // Preserve the native, unnormalized interpolated morph normal.
    return float4(color, _GbMaterialDiffuse.a * _TintColor.a);
}

half4 GameBoxTextureStages(half4 mainColor, half4 lightmap, float4 vertexColor, float hasLightmap)
{
    if (_GbNativeLighting < 0.5)
        return mainColor;
    vertexColor = saturate(vertexColor);
    if (hasLightmap > 0.5)
    {
        half3 firstStage = lightmap.rgb;
        if (_GbLightCount > 0.5 && _GbVertexColor < 0.5)
            firstStage = saturate(firstStage + vertexColor.rgb);
        return saturate(half4(firstStage * mainColor.rgb * 2,
            lightmap.a * mainColor.a * vertexColor.a * 2));
    }
    return saturate(mainColor * vertexColor);
}

#endif
