// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2025, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------
// Based on cician's shader from https://forum.unity3d.com/threads/simple-optimized-blur-shader.185327/#post-1267642
// Based on RoundedCorners.shader from https://github.com/kirevdokimov/Unity-UI-Rounded-Corners
// with some modifications

Shader "Pal3/RoundedFrostedGlass"
{
    Properties
    {
        _BlurAmount ("Blur", Range(0, 30)) = 1
        _Transparency ("Transparency", Range(0, 1)) = 1
        [HideInInspector] _MainTex ("Masking Texture", 2D) = "white" {}
        _AdditiveColor ("Additive Tint color", Color) = (0, 0, 0, 0)
        _MultiplyColor ("Multiply Tint color", Color) = (1, 1, 1, 1)
        _WidthHeightRadius ("WidthHeightRadius", Vector) = (0, 0, 0, 0)
    }

    Category
    {
        // We must be transparent, so other objects are drawn before this one.
        Tags { "Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent" }

        SubShader
        {
            Cull Off
            Lighting Off
            ZTest [unity_GUIZTestMode]
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha

            // Horizontal blur
            GrabPass
            {
                "_HBlur"
            }

            Pass
            {
                CGPROGRAM
                #pragma vertex vert
                #pragma fragment frag
                #pragma fragmentoption ARB_precision_hint_fastest
                #include "UnityCG.cginc"

                struct appdata_t {
                    float4 vertex : POSITION;
                    float2 texcoord : TEXCOORD0;
                };

                struct v2f {
                    float4 vertex : POSITION;
                    float4 uvgrab : TEXCOORD0;
                    float2 uvmain : TEXCOORD1;
                };

                sampler2D _MainTex;
                float4 _MainTex_ST;

                v2f vert (appdata_t v)
                {
                    v2f o;
                    o.vertex = UnityObjectToClipPos(v.vertex);

                    #if UNITY_UV_STARTS_AT_TOP
                    float scale = -1.0;
                    #else
                    float scale = 1.0;
                    #endif

                    o.uvgrab.xy = (float2(o.vertex.x, o.vertex.y * scale) + o.vertex.w) * 0.5;
                    o.uvgrab.zw = o.vertex.zw;

                    o.uvmain = TRANSFORM_TEX(v.texcoord, _MainTex);
                    return o;
                }

                inline float roundedRectangle(float2 samplePosition, float absoluteRound, float2 halfSize){
                    halfSize = halfSize - absoluteRound;
                    float2 distanceToEdge = abs(samplePosition) - halfSize;
                    const float outsideDistance = length(max(distanceToEdge, 0));
                    const float insideDistance = min(max(distanceToEdge.x, distanceToEdge.y), 0);
                    return outsideDistance + insideDistance - absoluteRound;
                }

                inline float calcAlpha(float2 samplePosition, float2 size, float radius){
                    const float2 samplePositionTranslated = (samplePosition - .5) * size;
                    const float distToRect = roundedRectangle(samplePositionTranslated, radius * .5, size * .5);
                    const float distanceChange = fwidth(distToRect) * 0.5;
                    return smoothstep(distanceChange, -distanceChange, distToRect);
                }

                inline fixed4 mixAlpha(fixed4 mainTexColor, fixed4 color, float sdfAlpha){
                    fixed4 col = mainTexColor * color;
                    col.a = min(col.a, sdfAlpha);
                    return col;
                }

                sampler2D _HBlur;
                float4 _HBlur_TexelSize;
                float _BlurAmount;
                float _Transparency;
                float4 _AdditiveColor;
                float4 _MultiplyColor;
                float4 _WidthHeightRadius;

                half4 frag(v2f i) : COLOR
                {
                    const float alpha = calcAlpha(i.uvmain, _WidthHeightRadius.xy, _WidthHeightRadius.z);
                    half4 cutoffColor = mixAlpha(tex2D(_MainTex, i.uvmain), _AdditiveColor, alpha);
                    clip(cutoffColor.a - 0.001);

                    half4 sum = half4(0,0,0,0);

                    #define GRABPIXEL(weight, kernelx) tex2Dproj( _HBlur, UNITY_PROJ_COORD(float4(i.uvgrab.x + _HBlur_TexelSize.x * kernelx * _BlurAmount, i.uvgrab.y, i.uvgrab.z, i.uvgrab.w))) * weight

                    sum += GRABPIXEL(0.05, -4.0);
                    sum += GRABPIXEL(0.09, -3.0);
                    sum += GRABPIXEL(0.12, -2.0);
                    sum += GRABPIXEL(0.15, -1.0);
                    sum += GRABPIXEL(0.18,  0.0);
                    sum += GRABPIXEL(0.15, +1.0);
                    sum += GRABPIXEL(0.12, +2.0);
                    sum += GRABPIXEL(0.09, +3.0);
                    sum += GRABPIXEL(0.05, +4.0);


                    half4 result = half4(sum.r * _MultiplyColor.r + _AdditiveColor.r,
                                        sum.g * _MultiplyColor.g + _AdditiveColor.g,
                                        sum.b * _MultiplyColor.b + _AdditiveColor.b,
                                        tex2D(_MainTex, i.uvmain).a);
                    result.a *= _Transparency;
                    return result;
                }
                ENDCG
            }

            // Vertical blur
            GrabPass
            {
                "_VBlur"
            }

            Pass
            {
                CGPROGRAM
                #pragma vertex vert
                #pragma fragment frag
                #pragma fragmentoption ARB_precision_hint_fastest
                #include "UnityCG.cginc"

                struct appdata_t {
                    float4 vertex : POSITION;
                    float2 texcoord: TEXCOORD0;
                };

                struct v2f {
                    float4 vertex : POSITION;
                    float4 uvgrab : TEXCOORD0;
                    float2 uvmain : TEXCOORD1;
                };

                sampler2D _MainTex;
                float4 _MainTex_ST;

                v2f vert(appdata_t v) {
                    v2f o;
                    o.vertex = UnityObjectToClipPos(v.vertex);

                    #if UNITY_UV_STARTS_AT_TOP
                    const float scale = -1.0;
                    #else
                    const float scale = 1.0;
                    #endif

                    o.uvgrab.xy = (float2(o.vertex.x, o.vertex.y * scale) + o.vertex.w) * 0.5;
                    o.uvgrab.zw = o.vertex.zw;

                    o.uvmain = TRANSFORM_TEX(v.texcoord, _MainTex);

                    return o;
                }

                inline float roundedRectangle(float2 samplePosition, float absoluteRound, float2 halfSize){
                    halfSize = halfSize - absoluteRound;
                    float2 distanceToEdge = abs(samplePosition) - halfSize;
                    const float outsideDistance = length(max(distanceToEdge, 0));
                    const float insideDistance = min(max(distanceToEdge.x, distanceToEdge.y), 0);
                    return outsideDistance + insideDistance - absoluteRound;
                }

                inline float calcAlpha(float2 samplePosition, float2 size, float radius){
                    const float2 samplePositionTranslated = (samplePosition - .5) * size;
                    const float distToRect = roundedRectangle(samplePositionTranslated, radius * .5, size * .5);
                    const float distanceChange = fwidth(distToRect) * 0.5;
                    return smoothstep(distanceChange, -distanceChange, distToRect);
                }

                inline fixed4 mixAlpha(fixed4 mainTexColor, fixed4 color, float sdfAlpha){
                    fixed4 col = mainTexColor * color;
                    col.a = min(col.a, sdfAlpha);
                    return col;
                }

                sampler2D _VBlur;
                float4 _VBlur_TexelSize;
                float _BlurAmount;
                float _Transparency;
                float4 _AdditiveColor;
                float4 _MultiplyColor;
                float4 _WidthHeightRadius;

                half4 frag(v2f i) : COLOR
                {
                    const float alpha = calcAlpha(i.uvmain, _WidthHeightRadius.xy, _WidthHeightRadius.z);
                    half4 cutoffColor = mixAlpha(tex2D(_MainTex, i.uvmain), _AdditiveColor, alpha);
                    clip(cutoffColor.a - 0.001);

                    half4 sum = half4(0,0,0,0);

                    #define GRABPIXEL(weight, kernely) tex2Dproj( _VBlur, UNITY_PROJ_COORD(float4(i.uvgrab.x, i.uvgrab.y + _VBlur_TexelSize.y * kernely * _BlurAmount, i.uvgrab.z, i.uvgrab.w))) * weight

                    sum += GRABPIXEL(0.05, -4.0);
                    sum += GRABPIXEL(0.09, -3.0);
                    sum += GRABPIXEL(0.12, -2.0);
                    sum += GRABPIXEL(0.15, -1.0);
                    sum += GRABPIXEL(0.18,  0.0);
                    sum += GRABPIXEL(0.15, +1.0);
                    sum += GRABPIXEL(0.12, +2.0);
                    sum += GRABPIXEL(0.09, +3.0);
                    sum += GRABPIXEL(0.05, +4.0);

                    half4 result = half4(sum.r * _MultiplyColor.r + _AdditiveColor.r,
                                        sum.g * _MultiplyColor.g + _AdditiveColor.g,
                                        sum.b * _MultiplyColor.b + _AdditiveColor.b,
                                        tex2D(_MainTex, i.uvmain).a);
                    result.a *= _Transparency;
                    return result;
                }
                ENDCG
            }
        }
    }
}