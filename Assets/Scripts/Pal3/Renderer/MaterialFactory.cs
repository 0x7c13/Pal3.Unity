
namespace Pal3.Renderer
{
    using UnityEngine;

    public static class MaterialFactory
    {
        public enum EMeshType
        {
            Poly,
            Mv3,
            Cvd,
        }

        private static string kOpaqueShaderPath = "Pal3/Opaque";
        private static string kTransparentShaderPath = "Pal3/Transparent";
        private static string kTransparentOpaquePartShaderPath = "Pal3/TransparentOpaquePart";

        private static readonly int _mainTexturePropertyId = Shader.PropertyToID("_MainTex");
        private static readonly int _tintColorPropertyId = Shader.PropertyToID("_TintColor");
        private static readonly int _transparentThresholdPropertyId = Shader.PropertyToID("_Threshold");
        private static readonly int _hasShadowTexPropertyId = Shader.PropertyToID("_HasShadowTex");
        private static readonly int _shadowTexturePropertyId = Shader.PropertyToID("_ShadowTex");


        public static Material[] CreateMaterials(EMeshType meshType,
                                                Texture2D mainTexture,
                                                Texture2D shadowTexture,
                                                Color tintColor,
                                                bool bTransparent,
                                                float transparentThreshold)
        {
            Material[] result = null;
            if (bTransparent)
            {
                result = new Material[2];
                result[0] = CreateTransparentOpaquePartMaterial(mainTexture,tintColor,transparentThreshold,shadowTexture);
                result[1] = CreateTransparentMaterial(mainTexture,tintColor,transparentThreshold,shadowTexture);
            }
            else
            {
                result = new Material[1];
                result[0] = CreateOpaqueMaterial(mainTexture,tintColor,shadowTexture);
            }
            return result;
        }
        
        private static Material CreateTransparentMaterial(Texture2D mainTexture,
                                                        Color tintColor,
                                                        float transparentThreshold,
                                                        Texture2D shadowTexture)
        {
            Material material = new Material(Shader.Find(kTransparentShaderPath));
            material.SetTexture(_mainTexturePropertyId,mainTexture);
            material.SetColor(_tintColorPropertyId, tintColor);
            material.SetFloat(_transparentThresholdPropertyId,transparentThreshold);
            
            // shadow
            material.SetFloat(_hasShadowTexPropertyId,0.0f);
            if (shadowTexture != null)
            {
                material.SetFloat(_hasShadowTexPropertyId,1.0f);
                material.SetTexture(_shadowTexturePropertyId,shadowTexture);
            }
            return material;
        }
        
        private static Material CreateTransparentOpaquePartMaterial(Texture2D mainTexture,
            Color tintColor,
            float transparentThreshold,
            Texture2D shadowTexture)
        {
            Material material = new Material(Shader.Find(kTransparentOpaquePartShaderPath));
            material.SetTexture(_mainTexturePropertyId,mainTexture);
            material.SetColor(_tintColorPropertyId, tintColor);
            material.SetFloat(_transparentThresholdPropertyId,transparentThreshold);
            
            // shadow
            material.SetFloat(_hasShadowTexPropertyId,0.0f);
            if (shadowTexture != null)
            {
                material.SetFloat(_hasShadowTexPropertyId,1.0f);
                material.SetTexture(_shadowTexturePropertyId,shadowTexture);
            }
            return material;
        }
        
        private static Material CreateOpaqueMaterial(Texture2D mainTexture,
                                                    Color tintColor,
                                                    Texture2D shadowTexture)
        {
            Material material = new Material(Shader.Find(kOpaqueShaderPath));
            material.SetTexture(_mainTexturePropertyId,mainTexture);
            material.SetColor(_tintColorPropertyId,tintColor);
            
            // shadow
            material.SetFloat(_hasShadowTexPropertyId,0.0f);
            if (shadowTexture != null)
            {
                material.SetFloat(_hasShadowTexPropertyId,1.0f);
                material.SetTexture(_shadowTexturePropertyId,shadowTexture);
            }
            
            return material;
        }


    }
}