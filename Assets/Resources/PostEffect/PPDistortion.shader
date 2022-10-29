/*
    reference: 
        https://www.shadertoy.com/view/MsKXDh
*/
Shader "Pal3/postprocess/Distortion"
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
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            /*
             https://docs.unity3d.com/462/Documentation/Manual/SL-BuiltinValues.html
             */
            //_Time	float4	Time (t/20, t, t*2, t*3), use to animate things inside the shaders.
            //_SinTime	float4	Sine of time: (t/8, t/4, t/2, t).
            //_CosTime	float4	Cosine of time: (t/8, t/4, t/2, t).

            // float4 _Time;
            // float4 _SineTime;
            // float4 _CosTime;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed2 uv = i.uv;
                float time = _Time.y;
                const float timeScale = 4.0;
                const float xFactor = 15.0;
                const float yFactor = 10.0;
                uv.x += sin(uv.y * xFactor + time * timeScale) / 400.0;
                uv.y += cos(uv.x * yFactor + time * timeScale) / 450.0;

                //uv.x += sin();
                uv.x += sin((uv.y+uv.x) * xFactor + time * timeScale) / (180. + (timeScale * sin(time)));
                uv.y += cos((uv.y+uv.x) * yFactor + time * timeScale) / (200. + (timeScale * sin(time)));
                            
                
                fixed4 col = tex2D(_MainTex, uv);
                
                return col;
            }
            ENDCG
        }
    }
}
