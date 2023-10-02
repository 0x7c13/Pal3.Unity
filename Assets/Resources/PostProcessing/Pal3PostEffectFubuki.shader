/*
    reference: https://www.shadertoy.com/view/4dl3R4 
*/
Shader "Pal3/PostEffectFubuki"
{
    Properties
    {
        
    }
    
    HLSLINCLUDE
        #include "Packages/com.unity.postprocessing/PostProcessing/Shaders/StdLib.hlsl"
        TEXTURE2D_SAMPLER2D(_MainTex, sampler_MainTex);
    
		#define vec2 float2
		#define vec3 float3
		#define vec4 float4
		#define mod fmod
		#define fract frac
    
		// This shader useds noise shaders by stegu -- http://webstaff.itn.liu.se/~stegu/
		// This is supposed to look like snow falling, for example like http://24.media.tumblr.com/tumblr_mdhvqrK2EJ1rcru73o1_500.gif

		vec2 mod289(vec2 x) {
		  return x - floor(x * (1.0 / 289.0)) * 289.0;
		}

		vec3 mod289(vec3 x) {
		  	return x - floor(x * (1.0 / 289.0)) * 289.0;
		}
		
		vec4 mod289(vec4 x) {
		  	return x - floor(x * (1.0 / 289.0)) * 289.0;
		}
		
		vec3 permute(vec3 x) {
		  return mod289(((x*34.0)+1.0)*x);
		}

		vec4 permute(vec4 x) {
		  return mod((34.0 * x + 1.0) * x, 289.0);
		}

		vec4 taylorInvSqrt(vec4 r)
		{
		  	return 1.79284291400159 - 0.85373472095314 * r;
		}
		
		float snoise(vec2 v)
		{
				const vec4 C = vec4(0.211324865405187,0.366025403784439,-0.577350269189626,0.024390243902439);
				vec2 i  = floor(v + dot(v, C.yy) );
				vec2 x0 = v -   i + dot(i, C.xx);
				
				vec2 i1;
				i1 = (x0.x > x0.y) ? vec2(1.0, 0.0) : vec2(0.0, 1.0);
				vec4 x12 = x0.xyxy + C.xxzz;
				x12.xy -= i1;
				
				i = mod289(i); // Avoid truncation effects in permutation
				vec3 p = permute( permute( i.y + vec3(0.0, i1.y, 1.0 ))
					+ i.x + vec3(0.0, i1.x, 1.0 ));
				
				vec3 m = max(0.5 - vec3(dot(x0,x0), dot(x12.xy,x12.xy), dot(x12.zw,x12.zw)), 0.0);
				m = m*m ;
				m = m*m ;
				
				vec3 x = 2.0 * fract(p * C.www) - 1.0;
				vec3 h = abs(x) - 0.5;
				vec3 ox = floor(x + 0.5);
				vec3 a0 = x - ox;
				
				m *= 1.79284291400159 - 0.85373472095314 * ( a0*a0 + h*h );
				
				vec3 g;
				g.x  = a0.x  * x0.x  + h.x  * x0.y;
				g.yz = a0.yz * x12.xz + h.yz * x12.yw;

				return 130.0 * dot(m, g);		
		}
		
		float cellular2x2(vec2 P)
		{
				#define K 0.142857142857 // 1/7
				#define K2 0.0714285714285 // K/2
				#define jitter 0.8 // jitter 1.0 makes F1 wrong more often
				
				vec2 Pi = mod(floor(P), 289.0);
				vec2 Pf = fract(P);
				vec4 Pfx = Pf.x + vec4(-0.5, -1.5, -0.5, -1.5);
				vec4 Pfy = Pf.y + vec4(-0.5, -0.5, -1.5, -1.5);
				vec4 p = permute(Pi.x + vec4(0.0, 1.0, 0.0, 1.0));
				p = permute(p + Pi.y + vec4(0.0, 0.0, 1.0, 1.0));
				vec4 ox = mod(p, 7.0)*K+K2;
				vec4 oy = mod(floor(p*K),7.0)*K+K2;
				vec4 dx = Pfx + jitter*ox;
				vec4 dy = Pfy + jitter*oy;
				vec4 d = dx * dx + dy * dy; // d11, d12, d21 and d22, squared
				// Sort out the two smallest distances
				
				// Cheat and pick only F1
				d.xy = min(d.xy, d.zw);
				d.x = min(d.x, d.y);
				return d.x; // F1 duplicated, F2 not computed
		}

		float fbm(vec2 p) {
 		   float f = 0.0;
    		float w = 0.5;
    		for (int i = 0; i < 5; i ++) {
						f += w * snoise(p);
						p *= 2.;
						w *= 0.5;
    		}
    		return f;
		}

        float4 Frag(VaryingsDefault i) : SV_Target
        {
				float2 uv = i.texcoord;
        		float2 originUV = uv;
				const float time = _Time.y;
        	
				float speed=2.0;
        	
				// vec2 uv = fragCoord.xy / iResolution.xy;
				// uv.x*=(iResolution.x/iResolution.y);
				uv.x *= _ScreenParams.x / _ScreenParams.y;
        		
										
				vec2 GA = float2(0.0,0.0);
				GA.x-=time*1.8;
				GA.y+=time*0.9;
				GA*=speed;
			
				float F1=0.0,F2=0.0,F3=0.0,F4=0.0,F5=0.0,N1=0.0,N2=0.0,N3=0.0,N4=0.0,N5=0.0;
				float A=0.0,A1=0.0,A2=0.0,A3=0.0,A4=0.0,A5=0.0;


				// Attentuation
				A = (uv.x-(uv.y*0.3));
				A = clamp(A,0.0,1.0);

				// Snow layers, somewhat like an fbm with worley layers.
				F1 = 1.0-cellular2x2((uv+(GA*0.1))*8.0);	
				A1 = 1.0-(A*1.0);
				N1 = smoothstep(0.998,1.0,F1)*1.0*A1;	

				F2 = 1.0-cellular2x2((uv+(GA*0.2))*6.0);	
				A2 = 1.0-(A*0.8);
				N2 = smoothstep(0.995,1.0,F2)*0.85*A2;				

				F3 = 1.0-cellular2x2((uv+(GA*0.4))*4.0);	
				A3 = 1.0-(A*0.6);
				N3 = smoothstep(0.99,1.0,F3)*0.65*A3;				

				F4 = 1.0-cellular2x2((uv+(GA*0.6))*3.0);	
				A4 = 1.0-(A*1.0);
				N4 = smoothstep(0.98,1.0,F4)*0.4*A4;				

				F5 = 1.0-cellular2x2((uv+(GA))*1.2);	
				A5 = 1.0-(A*1.0);
				N5 = smoothstep(0.98,1.0,F5)*0.25*A5;				
								
				float Snowout=N5+N4+N3+N2+N1;
        	
        		float4 snowColor = vec4(Snowout*0.9, Snowout, Snowout*1.1, 1.0);
        		float4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex,originUV);
				return col + snowColor;
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
