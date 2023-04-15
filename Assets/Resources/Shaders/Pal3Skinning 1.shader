Shader "Pal3/SkinningCPU"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;

                //float4 boneIds : TEXCOORD1;
                //float4 boneWeights : TEXCOORD2;
                float4 color : TEXCOORD1;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;

                //float4 boneIds : TEXCOORD1;
                //float4 boneWeights : TEXCOORD2;
                float4 color : TEXCOORD1;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            
            
            v2f vert (appdata v)
            {
                v2f o;

                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                
                o.color = v.color;
                //o.boneIds = v.boneIds;
                //o.boneWeights = v.boneWeights;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                //fixed4 col = tex2D(_MainTex, i.uv);
                //col *= float4(0.1,0.33,0.8,1.0);
                fixed4 col = i.color; 
                return col;
            }
            ENDCG
        }
    }
}
