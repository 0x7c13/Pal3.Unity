Shader "Pal3/Water"
{
    Properties
    {
        // main texture
        _MainTex ("Main texture", 2D) = "white" {}
        
        // alpha value
        _Alpha ("Alpha",Range(0, 1)) = 0.5
        
        // pre baked shadow texture
        _ShadowTex ("Shadow texture", 2D) = "white" {}
        _HasShadowTex ("Has Shadow Texture", Range(0, 1)) = 0.0
        
        // tint color
        _TintColor ("Tint color", Color) = (1.0, 1.0, 1.0, 1.0)
        
        [Enum(UnityEngine.Rendering.BlendMode)]
        _BlendSrcFactor("Source Blend Factor", int) = 5    // BlendMode.SrcAlpha as Default
        
        [Enum(UnityEngine.Rendering.BlendMode)]
        _BlendDstFactor("Dest Blend Factor", int) = 10     // BlendMode.OneMinusSrcAlpha as Default
    }
    
    SubShader
    {
        Lighting Off
        Tags { "QUEUE" = "Transparent" }
        
        Pass
        {
            Blend [_BlendSrcFactor] [_BlendDstFactor]
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
                float2 texcoord2 : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 texcoord : TEXCOORD0;
                float2 texcoord2 : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };
            
            sampler2D _MainTex;
            float4 _MainTex_ST;

            float _Alpha;
            
            float4 _TintColor;
            
            float _HasShadowTex;
            sampler2D _ShadowTex;
            float4 _ShadowTex_ST;
            
            v2f vert(appdata_t v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                o.texcoord2 = TRANSFORM_TEX(v.texcoord2, _ShadowTex);
                
                return o;
            }

            half4 frag(v2f i) : SV_Target
            {
                const half4 color = tex2D(_MainTex, i.texcoord);
                const half4 colorShadow = tex2D(_ShadowTex, i.texcoord2);
                half4 mixedColor = color * colorShadow * _TintColor;
                mixedColor.a = _Alpha; // use global alpha
                return mixedColor;
            }
            ENDCG
        }
    }
}