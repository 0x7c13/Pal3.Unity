
namespace Pal3.Renderer
{
    using System;
    using UnityEngine;

    public class MaterialManager : IDisposable
    {
        public enum EMeshType
        {
            Poly,
            Mv3,
            Cvd,
        }
        
        public enum EBlendMode
        {
            Opaque,             // don't blend
            AlphaBlend,         // src.alpha, 1-src.alpha, +
            InvertColorBlend,   // srcAlpha, One , + 
        }

        private static readonly string kOpaqueShaderPath = "Pal3/Opaque";
        private static readonly string kTransparentShaderPath = "Pal3/Transparent";
        private static readonly string kTransparentOpaquePartShaderPath = "Pal3/TransparentOpaquePart";
        private static readonly string kSpriteShaderPath = "Pal3/Sprite";
        
        
        // Standard material uniforms 
        private static readonly int MainTexturePropertyId = Shader.PropertyToID("_MainTex");
        private static readonly int TintColorPropertyId = Shader.PropertyToID("_TintColor");
        private static readonly int TransparentThresholdPropertyId = Shader.PropertyToID("_Threshold");
        private static readonly int HasShadowTexPropertyId = Shader.PropertyToID("_HasShadowTex");
        private static readonly int ShadowTexturePropertyId = Shader.PropertyToID("_ShadowTex");
        
        private static readonly int BlendSrcFactorPropertyId = Shader.PropertyToID("_BlendSrcFactor");
        private static readonly int BlendDstFactorPropertyId = Shader.PropertyToID("_BlendDstFactor");
        
        // Sprite material uniforms
        private static readonly int SpriteMainTexPropertyId = Shader.PropertyToID("_MainTex");
        
        public Material CreateSpriteMaterial(Texture2D texture)
        {
            Material material = new Material(Shader.Find(kSpriteShaderPath));;
            material.SetTexture(SpriteMainTexPropertyId,texture);
            return material;
        }

        public Material[] CreateStandardMaterials(EMeshType meshType,
                                                Texture2D mainTexture,
                                                Texture2D shadowTexture,
                                                Color tintColor,
                                                EBlendMode blendMode,
                                                float transparentThreshold)
        {
            bool bTransparent = (blendMode == EBlendMode.AlphaBlend || blendMode == EBlendMode.InvertColorBlend);
            
            Material[] result = null;
            if (bTransparent)
            {
                result = new Material[2];
                result[0] = CreateTransparentOpaquePartMaterial(mainTexture,tintColor,transparentThreshold,shadowTexture);
                result[1] = CreateTransparentMaterial(mainTexture,tintColor,transparentThreshold,shadowTexture);


                var srcFactor = UnityEngine.Rendering.BlendMode.SrcAlpha;
                var destFactor = UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha;
                if (blendMode == EBlendMode.InvertColorBlend)
                {
                    srcFactor = UnityEngine.Rendering.BlendMode.SrcAlpha;
                    destFactor = UnityEngine.Rendering.BlendMode.One;
                }
                
                result[1].SetInt(BlendSrcFactorPropertyId,(int)srcFactor);
                result[1].SetInt(BlendDstFactorPropertyId,(int)destFactor);
                //result[1].SetInt(BlendSrcFactorPropertyId,0);
                //result[1].SetInt(BlendDstFactorPropertyId,0);
            }
            else
            {
                result = new Material[1];
                result[0] = CreateOpaqueMaterial(mainTexture,tintColor,shadowTexture);
            }
            return result;
        }
        
        private Material CreateTransparentMaterial(Texture2D mainTexture,
                                                        Color tintColor,
                                                        float transparentThreshold,
                                                        Texture2D shadowTexture)
        {
            Material material = new Material(Shader.Find(kTransparentShaderPath));
            material.SetTexture(MainTexturePropertyId,mainTexture);
            material.SetColor(TintColorPropertyId, tintColor);
            material.SetFloat(TransparentThresholdPropertyId,transparentThreshold);
            
            // shadow
            material.SetFloat(HasShadowTexPropertyId,0.0f);
            if (shadowTexture != null)
            {
                material.SetFloat(HasShadowTexPropertyId,1.0f);
                material.SetTexture(ShadowTexturePropertyId,shadowTexture);
            }
            return material;
        }
        
        private Material CreateTransparentOpaquePartMaterial(Texture2D mainTexture,
            Color tintColor,
            float transparentThreshold,
            Texture2D shadowTexture)
        {
            Material material = new Material(Shader.Find(kTransparentOpaquePartShaderPath));
            material.SetTexture(MainTexturePropertyId,mainTexture);
            material.SetColor(TintColorPropertyId, tintColor);
            material.SetFloat(TransparentThresholdPropertyId,transparentThreshold);
            
            // shadow
            material.SetFloat(HasShadowTexPropertyId,0.0f);
            if (shadowTexture != null)
            {
                material.SetFloat(HasShadowTexPropertyId,1.0f);
                material.SetTexture(ShadowTexturePropertyId,shadowTexture);
            }
            return material;
        }
        
        private static Material CreateOpaqueMaterial(Texture2D mainTexture,
                                                    Color tintColor,
                                                    Texture2D shadowTexture)
        {
            Material material = new Material(Shader.Find(kOpaqueShaderPath));
            material.SetTexture(MainTexturePropertyId,mainTexture);
            material.SetColor(TintColorPropertyId,tintColor);
            
            // shadow
            material.SetFloat(HasShadowTexPropertyId,0.0f);
            if (shadowTexture != null)
            {
                material.SetFloat(HasShadowTexPropertyId,1.0f);
                material.SetTexture(ShadowTexturePropertyId,shadowTexture);
            }
            
            return material;
        }


        public void Dispose()
        {
            // @miao @todo
        }
    }
}