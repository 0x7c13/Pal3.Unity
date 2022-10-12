
namespace Pal3.Renderer
{
    using UnityEngine;

    public class MaterialFactory
    {

        private static string kOpaqueShaderPath = "Pal3/Opaque";
        private static string kTransparentShaderPath = "Pal3/Transparent";
        
        private static readonly int _mainTexturePropertyId = Shader.PropertyToID("_MainTex");
        private static readonly int _tintColorPropertyId = Shader.PropertyToID("_TintColor");
        private static readonly int _transparentThresholdPropertyId = Shader.PropertyToID("_Threshold");
        private static readonly int _hasShadowTexPropertyId = Shader.PropertyToID("_HasShadowTex");
        private static readonly int _shadowTexturePropertyId = Shader.PropertyToID("_ShadowTex");

        public static Material CreateTransparentMaterial(Texture2D mainTexture,
                                                        Color tintColor,
                                                        float transparentThreshold,
                                                        Texture2D shadowTexture = null)
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
        
        public static Material CreateOpaqueMaterial(Texture2D mainTexture,
                                                    Color tintColor,
                                                    Texture2D shadowTexture = null)
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