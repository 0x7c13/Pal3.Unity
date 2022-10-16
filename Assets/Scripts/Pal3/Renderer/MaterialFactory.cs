// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Renderer
{
    using Core.GameBox;
    using UnityEngine;
    using UnityEngine.Rendering;

    public class MaterialFactory : IMaterialFactory
    {
        private const string OPAQUE_SHADER_PATH = "Pal3/Opaque";
        private const string TRANSPARENT_SHADER_PATH = "Pal3/Transparent";
        private const string TRANSPARENT_OPAQUE_PART_SHADER_PATH = "Pal3/TransparentOpaquePart";
        private const string SPRITE_SHADER_PATH = "Pal3/Sprite";
        private const string WATER_SHADER_PATH = "Pal3/Water";

        // Standard material uniforms 
        private static readonly int MainTexturePropertyId = Shader.PropertyToID("_MainTex");
        private static readonly int TintColorPropertyId = Shader.PropertyToID("_TintColor");
        private static readonly int TransparentThresholdPropertyId = Shader.PropertyToID("_Threshold");
        private static readonly int HasShadowTexturePropertyId = Shader.PropertyToID("_HasShadowTex");
        private static readonly int ShadowTexturePropertyId = Shader.PropertyToID("_ShadowTex");
        
        private static readonly int BlendSrcFactorPropertyId = Shader.PropertyToID("_BlendSrcFactor");
        private static readonly int BlendDstFactorPropertyId = Shader.PropertyToID("_BlendDstFactor");
        
        // Sprite material uniforms
        private static readonly int SpriteMainTexPropertyId = Shader.PropertyToID("_MainTex");
        
        // Water material uniforms
        private static readonly int WaterMainTexPropId = Shader.PropertyToID("_MainTex");
        private static readonly int WaterShadowTexPropId = Shader.PropertyToID("_ShadowTex");
        private static readonly int WaterAlphaPropId = Shader.PropertyToID("_Alpha");
        private static readonly int WaterHasShadowTexPropId = Shader.PropertyToID("_HasShadowTex");

        /// <inheritdoc/>
        public Material CreateWaterMaterial(Texture2D mainTexture, 
            Texture2D shadowTexture, 
            float alpha)
        {
            var material = new Material(Shader.Find(WATER_SHADER_PATH));
            material.SetTexture(WaterMainTexPropId,mainTexture);
            material.SetFloat(WaterAlphaPropId, alpha);
            if (shadowTexture != null)
            {
                material.SetFloat(WaterHasShadowTexPropId, 1.0f);
                material.SetTexture(WaterShadowTexPropId, shadowTexture);
            }
            else
            {
                material.SetFloat(WaterHasShadowTexPropId, 0.5f);
            }
            return material;
        }

        /// <inheritdoc/>
        public Material CreateSpriteMaterial(Texture2D texture)
        {
            var material = new Material(Shader.Find(SPRITE_SHADER_PATH));;
            material.SetTexture(SpriteMainTexPropertyId, texture);
            return material;
        }

        /// <inheritdoc/>
        public Material[] CreateStandardMaterials(
            Texture2D mainTexture,
            Texture2D shadowTexture,
            Color tintColor,
            GameBoxBlendFlag blendFlag,
            float transparentThreshold)
        {
            Material[] materials = null;
            
            if (blendFlag is GameBoxBlendFlag.AlphaBlend or GameBoxBlendFlag.InvertColorBlend)
            {
                materials = new Material[2];
                materials[0] = CreateTransparentOpaquePartMaterial(mainTexture,
                    tintColor,
                    transparentThreshold,
                    shadowTexture);
                materials[1] = CreateTransparentMaterial(mainTexture,
                    tintColor,
                    transparentThreshold,
                    shadowTexture);

                BlendMode srcFactor;
                BlendMode destFactor;
                
                if (blendFlag == GameBoxBlendFlag.InvertColorBlend)
                {
                    srcFactor = BlendMode.SrcAlpha;
                    destFactor = BlendMode.One;
                }
                else
                {
                    srcFactor = BlendMode.SrcAlpha;
                    destFactor = BlendMode.OneMinusSrcAlpha;
                }
                
                materials[1].SetInt(BlendSrcFactorPropertyId, (int)srcFactor);
                materials[1].SetInt(BlendDstFactorPropertyId, (int)destFactor);
            }
            else if (blendFlag == GameBoxBlendFlag.Opaque)
            {
                materials = new Material[1];
                materials[0] = CreateOpaqueMaterial(mainTexture, tintColor, shadowTexture);
            }
            
            return materials;
        }
        
        private static Material CreateTransparentMaterial(Texture2D mainTexture,
            Color tintColor,
            float transparentThreshold,
            Texture2D shadowTexture)
        {
            var material = new Material(Shader.Find(TRANSPARENT_SHADER_PATH));
            material.SetTexture(MainTexturePropertyId, mainTexture);
            material.SetColor(TintColorPropertyId, tintColor);
            material.SetFloat(TransparentThresholdPropertyId, transparentThreshold);
            
            // shadow texture
            material.SetFloat(HasShadowTexturePropertyId, 0.0f);
            if (shadowTexture != null)
            {
                material.SetFloat(HasShadowTexturePropertyId, 1.0f);
                material.SetTexture(ShadowTexturePropertyId, shadowTexture);
            }
            
            return material;
        }
        
        private static Material CreateTransparentOpaquePartMaterial(Texture2D mainTexture,
            Color tintColor,
            float transparentThreshold,
            Texture2D shadowTexture)
        {
            Material material = new Material(Shader.Find(TRANSPARENT_OPAQUE_PART_SHADER_PATH));
            material.SetTexture(MainTexturePropertyId, mainTexture);
            material.SetColor(TintColorPropertyId, tintColor);
            material.SetFloat(TransparentThresholdPropertyId, transparentThreshold);
            
            // shadow
            material.SetFloat(HasShadowTexturePropertyId, 0.0f);
            if (shadowTexture != null)
            {
                material.SetFloat(HasShadowTexturePropertyId, 1.0f);
                material.SetTexture(ShadowTexturePropertyId, shadowTexture);
            }
            
            return material;
        }
        
        private static Material CreateOpaqueMaterial(Texture2D mainTexture,
            Color tintColor,
            Texture2D shadowTexture)
        {
            Material material = new Material(Shader.Find(OPAQUE_SHADER_PATH));
            material.SetTexture(MainTexturePropertyId, mainTexture);
            material.SetColor(TintColorPropertyId, tintColor);
            
            // shadow
            material.SetFloat(HasShadowTexturePropertyId, 0.0f);
            if (shadowTexture != null)
            {
                material.SetFloat(HasShadowTexturePropertyId, 1.0f);
                material.SetTexture(ShadowTexturePropertyId, shadowTexture);
            }
            
            return material;
        }
    }
}