/*
    reference: 
        https://www.shadertoy.com/view/MsKXDh
*/
Shader "Pal3/postprocess/Distortion"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        
        _TimeScale ("Time Scale",float) = 4.0
        _XFactor ("X Factor",float) = 15.0
        _YFactor("Y Factor",float) = 10.0
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

            float _TimeScale;
            float _XFactor;
            float _YFactor;
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
                
                
                uv.x += sin(uv.y * _XFactor + time * _TimeScale) / 400.0;
                uv.y += cos(uv.x * _YFactor + time * _TimeScale) / 450.0;

                //uv.x += sin();
                uv.x += sin((uv.y+uv.x) * _XFactor + time * _TimeScale) / (180. + (_TimeScale * sin(time)));
                uv.y += cos((uv.y+uv.x) * _YFactor + time * _TimeScale) / (200. + (_TimeScale * sin(time)));
                            
                
                fixed4 col = tex2D(_MainTex, uv);
                
                return col;
            }
            ENDCG
        }
    }
}
