// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

Shader "Pal3/StandardNoShadowTransparent"
{
    Properties
    {
        _MainTex ("Base (RGB) Trans (A)", 2D) = "white" {}
        _Cutoff ("Alpha cutoff", Range(0,1)) = 0.0
        _TintColor ("Tint color", Color) = (1.0, 1.0, 1.0, 1.0)
        _IsOpaque("Is Material Opaque", Range(0,1)) = 1.0
    }
    SubShader
    {
        Tags { "Queue"="Geometry" "IgnoreProjector"="True" "RenderType"="TransparentCutout" }
        LOD 100

        Lighting Off
        
        // Pass 1, rendering Opaque part.Blend Off for performance 
        Pass
        {
            Blend Off 
            ZWrite On
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            

            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 texcoord : TEXCOORD0;
                
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed _Cutoff;
            half4 _TintColor;
            float _IsOpaque;

            v2f vert(appdata_t v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                
                return o;
            }

            half4 frag(v2f i) : SV_Target
            {
                half4 color = tex2D(_MainTex, i.texcoord);
                clip(color.a - 1.0);
                return color;
            }
            ENDCG
        }
        
        // Pass 2, rendering transparent part.ZWrite Off for transparent effect
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            

            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 texcoord : TEXCOORD0;
                
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed _Cutoff;
            half4 _TintColor;
            float _IsOpaque;

            v2f vert(appdata_t v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                
                return o;
            }

            half4 frag(v2f i) : SV_Target
            {
                half4 color = tex2D(_MainTex, i.texcoord);
                if(color.a >= 1.0)
                {
                    discard;
                }
                return color;
            }
            ENDCG
        }
        
    }
}