Shader "Pal3/Skinning"
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
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;

                float4 boneIds : TEXCOORD1;
                float4 boneWeights : TEXCOORD2;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;

                float4 boneIds : TEXCOORD1;
                float4 boneWeights : TEXCOORD2;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            
            uniform float4x4 _boneMatrixArray[50];

            v2f vert (appdata v)
            {
                v2f o;

                //o.vertex = UnityObjectToClipPos(v.vertex);
                
                
                float4x4 boneMatrix = _boneMatrixArray[(int)(v.boneIds[0])] * v.boneWeights[0];
                boneMatrix += _boneMatrixArray[(int)(v.boneIds[1])] * v.boneWeights[1];
                boneMatrix += _boneMatrixArray[(int)(v.boneIds[2])] * v.boneWeights[2];
                boneMatrix += _boneMatrixArray[(int)(v.boneIds[3])] * v.boneWeights[3];
                
                float4 boneVertexPos = mul(boneMatrix,v.vertex);
                o.vertex = UnityObjectToClipPos(boneVertexPos);
                
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);

                o.boneIds = v.boneIds;
                o.boneWeights = v.boneWeights;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);
                col *= float4(0.54,0.9,0.7,1.0);
                return col;
            }
            ENDCG
        }
    }
}
