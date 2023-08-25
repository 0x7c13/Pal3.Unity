/*
    reference: https://www.shadertoy.com/view/ldsGDn
*/
Shader "Pal3/PostEffectSnow"
{
    Properties
    {
        // Blizzard
        // #define LAYERS 200
	    // #define DEPTH .1
	    // #define WIDTH .8
	    // #define SPEED 1.5
        
        // Light Snow
    	// #define LAYERS 50
	    // #define DEPTH .5
	    // #define WIDTH .3
	    // #define SPEED .6
        
        _Layers ("Layers",int) = 50
        _Depth ("Depth",float) = 0.5
        _Width("Width",float) = 0.3
        _Speed("Speed",float) = 0.6
    }
    
    HLSLINCLUDE
        #include "Packages/com.unity.postprocessing/PostProcessing/Shaders/StdLib.hlsl"
        TEXTURE2D_SAMPLER2D(_MainTex, sampler_MainTex);

        int _Layers;
        float _Depth;
        float _Width;
        float _Speed; 
    
        float4 Frag(VaryingsDefault i) : SV_Target
        {
            float2 uv = i.texcoord;
            const float time = _Time.y;
            float3x3 p = float3x3(13.323122,23.5112,21.71123,21.1212,28.7312,11.9312,21.8112,14.7212,61.3934);
            float3 acc = float3(0.0,0.0,0.0);

            float dof = 5.0 * sin(time * 0.1);
            
            for(int i = 0;i < _Layers;i++)
            {
                float fi = float(i);
                float2 q = uv * (1.0 + fi * _Depth);
                q += float2(q.y*(_Width * fmod(fi*7.238917,1.)-_Width*.5),_Speed * time/(1.+fi*_Depth*.03));
                
                float3 n = float3(floor(q),31.189 + fi);
                float3 m = floor(n) * 0.00001 + frac(n);
                
                //float3 temp = m * p;//p * m;
                float3 temp = mul(p,m);
                
                float temp1 = 31415.9;
                float3 mp = (float3(temp1,temp1,temp1) + m) / frac(temp);
                
                float3 r = frac(mp);
                float2 s = abs(fmod(q,1.)-.5+.9*r.xy-.45);
                s += .01*abs(2.*frac(10.*q.yx)-1.); 

                float d = .6*max(s.x-s.y,s.x+s.y)+max(s.x,s.y)-.01;

		        float edge = .005+.05*min(.5*abs(fi-5.-dof),1.);

                float temp2 = smoothstep(edge,-edge,d);
                float temp3 = r.x / (1.0 + 0.02 * fi * _Depth);
                float temp4 = temp2 * temp3;
                
                acc += float3(temp4,temp4,temp4);
            }
            
            return SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex,uv) + float4(float3(acc),1.0);
        }
    ENDHLSL
     
    SubShader
    {
      Cull Off ZWrite Off ZTest Always
      Pass
      {
          HLSLPROGRAM
              #pragma vertex VertDefault
              #pragma fragment Frag
          ENDHLSL
      }
    }
}
