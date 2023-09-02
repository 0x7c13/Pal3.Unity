Shader "Pal3/Effect/VisionRange"
{
    Properties
    {
        _Angle("Angle",float) = 0.785 // PI * 1/4
        _FrontDir("FrontDir",Vector) = (1,0,0,0)
        _DepthTex ("DepthTex", 2D) = "green" {}
    }
    SubShader
    {
        //Tags { "RenderType"="Opaque" }
        Tags {"Queue" = "Transparent"}
        LOD 100

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off

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
                float4 worldPos : SV_Target0;
            };
            
            float _Angle;
            float4 _FrontDir;

            sampler2D _DepthTex;
            
            float4x4 _depthCameraViewMatrix;
            float4x4 _depthCameraProjMatrix;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.worldPos = mul(unity_ObjectToWorld,v.vertex);
                return o;
            }
            
            float4 getDepthTexUVByWorldPos(float4 worldPos)
            {
                float4 clipPos = mul(_depthCameraProjMatrix,mul(_depthCameraViewMatrix,worldPos));
                float4 ndcPos = clipPos / clipPos.w;
                float4 uvPos = ndcPos * 0.5 + 0.5;
                return uvPos;
            }

            float getDepthValueByDepthTex(float4 worldPos)
            {
                float2 depthUV = getDepthTexUVByWorldPos(worldPos).xy;   
                float depth = SAMPLE_DEPTH_TEXTURE(_DepthTex,depthUV);
                depth = Linear01Depth(depth);
                return depth;
            }
            
            float getDepthValueByDepthTex2(float4 worldPos)
            {
                const float2 depthUV = getDepthTexUVByWorldPos(worldPos).xy;   
                float depth = SAMPLE_DEPTH_TEXTURE(_DepthTex,depthUV);
                return depth;
            }
            
            
            float getDepthValueByWorldPos(float4 worldPos)
            {
                float depth = getDepthTexUVByWorldPos(worldPos).z;
                depth = Linear01Depth(depth);
                return depth;
            }

            float getDepthValueByWorldPos2(float4 worldPos)
            {
                float depth = getDepthTexUVByWorldPos(worldPos).z;
                return depth;
            }

            // Display whole rect 
            fixed4 fragV1(v2f i)
            {
                return fixed4(1,0,0,1);   
            }

            // Display circle
            fixed4 fragV2(v2f i)
            {
                float2 uv = i.uv * 2.0 - 1.0;
                if(length(uv) > 1.0)
                {
                    discard;
                }
                return fixed4(1,0,0,1);
            }

            // Display Fan
            fixed4 fragV3(v2f i)
            {
                float2 uv = i.uv * 2.0 - 1.0;
                
                float2 frontDirIn2D = normalize(float2(_FrontDir.x,_FrontDir.z));
                float2 uvDir = normalize(uv);
                float dotValue = dot(uvDir,frontDirIn2D);
                float angle = acos(dotValue);

                if(length(uv) > 1.0 || angle > _Angle * 0.5)
                {
                    discard;
                }
                return float4(1,0,0,1);
            }

            // Test for depth texture
            fixed4 fragDebug(v2f i)
            {
                // test matrix
                // return float4(_depthCameraProjMatrix[0]);
                
                // test depth texture
                float2 originUV = i.uv;
                float4 colorFromRT = tex2D(_DepthTex,originUV);
                return float4(colorFromRT.rgb,1.0);
            }
            
            
            fixed4 frag (v2f i) : SV_Target
            {
                //return fragV1(i);
                //return fragV2(i);
                //return fragV3(i);
                //return fragDebug(i);

                // Discard not in Fan part 
                float2 uv = i.uv * 2.0 - 1.0;
                float2 frontDirIn2D = normalize(float2(_FrontDir.x,_FrontDir.z));
                float2 uvDir = normalize(uv);
                float dotValue = dot(uvDir,frontDirIn2D);
                float angle = acos(dotValue);

                if(length(uv) > 1.0 || angle > _Angle * 0.5)
                {
                    discard;
                }

                // Depth comparision
                const float4 worldPos = i.worldPos / i.worldPos.w;

                // depth value from depth texture 
                float v1 = 1 - getDepthValueByDepthTex2(worldPos);  

                // depth value from world pos
                float v2 = getDepthValueByWorldPos2(worldPos);
                
                if(v2 > v1)
                {
                    discard;
                }

                return float4(0,1,0,0.5);
            }
            ENDCG
        }
    }
}
