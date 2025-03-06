// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2025, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

Shader "Pal3/Water"
{
    Properties
    {
        // main texture
        _MainTex ("Texture", 2D) = "white" {}

        // alpha value
        _Alpha ("Alpha",Range(0, 1)) = 0.5

        _Exposure("Exposure Amount", Range(0.1, 1.0)) = 0.3

        // pre baked shadow texture
        _ShadowTex ("Shadow texture", 2D) = "white" {}
        _HasShadowTex ("Has Shadow Texture", Range(0, 1)) = 0.0

        [Enum(UnityEngine.Rendering.BlendMode)]
        _BlendSrcFactor("Source Blend Factor", int) = 5    // BlendMode.SrcAlpha as Default

        [Enum(UnityEngine.Rendering.BlendMode)]
        _BlendDstFactor("Dest Blend Factor", int) = 10     // BlendMode.OneMinusSrcAlpha as Default
    }

    SubShader
    {
        Lighting Off

        Tags { "QUEUE" = "Transparent-1" } // -1 to fix sorting order issue with semi-transparent trees etc.

        Pass
        {
            Blend [_BlendSrcFactor] [_BlendDstFactor]
            ZWrite Off

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float2 texcoord0 : TEXCOORD0;
                float2 texcoord1 : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 texcoord0 : TEXCOORD0;
                float2 texcoord1 : TEXCOORD1;
                UNITY_FOG_COORDS(7)
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            float _Alpha;
            float _Exposure;

            float _HasShadowTex;
            sampler2D _ShadowTex;
            float4 _ShadowTex_ST;

            v2f vert(appdata_t v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.texcoord0 = TRANSFORM_TEX(v.texcoord0, _MainTex);
                o.texcoord1 = TRANSFORM_TEX(v.texcoord1, _ShadowTex);
                UNITY_TRANSFER_FOG(o, o.vertex);
                return o;
            }

            half4 frag(v2f i) : SV_Target
            {
                const half4 color = tex2D(_MainTex, i.texcoord0);
                const half4 colorShadow = tex2D(_ShadowTex, i.texcoord1);
                half4 mixedColor = color * colorShadow / (1 - _Exposure);
                mixedColor.a = _Alpha; // use global alpha
                UNITY_APPLY_FOG(i.fogCoord, mixedColor);
                return mixedColor;
            }
            ENDCG
        }
    }
}