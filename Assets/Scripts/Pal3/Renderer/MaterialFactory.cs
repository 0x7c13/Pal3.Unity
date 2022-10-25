// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Renderer
{
    using System.Collections.Generic;
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
        
        private const string REALTOON_DEFAULT_SHADER_PATH = "Pal3/RealToon/Default";
        private const string REALTOON_TRANSPARENT_SHADER_PATH = "Pal3/RealToon/Transparent";

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

        private static readonly Dictionary<string, Shader> Shaders = new ();

        private static Shader GetShader(string shaderName)
        {
            if (Shaders.ContainsKey(shaderName)) return Shaders[shaderName];

            var shader = Shader.Find(shaderName);
            
            if (shader != null)
            {
                Shaders[shaderName] = shader;
            }
            
            return shader;
        }
        
        /// <inheritdoc/>
        public Material CreateWaterMaterial(Texture2D mainTexture, 
            Texture2D shadowTexture, 
            float alpha)
        {
            var material = new Material(GetShader(WATER_SHADER_PATH));
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
        public Material CreateSpriteMaterial()
        {
            return new Material(GetShader(SPRITE_SHADER_PATH));
        }

        /// <inheritdoc/>
        public Material CreateSpriteMaterial(Texture2D texture)
        {
            var material = new Material(GetShader(SPRITE_SHADER_PATH));
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
            
            #if RTX_ON
            if (blendFlag is GameBoxBlendFlag.AlphaBlend or GameBoxBlendFlag.InvertColorBlend)
            {
                materials = new Material[1];
                materials[0] = CreateTransparentMaterial(mainTexture, tintColor, transparentThreshold, shadowTexture);
            }
            else if (blendFlag == GameBoxBlendFlag.Opaque)
            {
                materials = new Material[1];
                materials[0] = CreateOpaqueMaterial(mainTexture, tintColor, shadowTexture);
            }
            #else
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
            #endif
            
            return materials;
        }
        
        private static Material CreateTransparentMaterial(Texture2D mainTexture,
            Color tintColor,
            float transparentThreshold,
            Texture2D shadowTexture)
        {
            #if RTX_ON
            Material material = new Material(GetShader(REALTOON_TRANSPARENT_SHADER_PATH));
            material.mainTexture = mainTexture;
            return material;
            #else
            var material = new Material(GetShader(TRANSPARENT_SHADER_PATH));
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
            #endif
        }
        
        private static Material CreateTransparentOpaquePartMaterial(Texture2D mainTexture,
            Color tintColor,
            float transparentThreshold,
            Texture2D shadowTexture)
        {
            #if RTX_ON
            Material material = new Material(GetShader(REALTOON_TRANSPARENT_SHADER_PATH));
            material.mainTexture = mainTexture;
            return material;
            #else
            Material material = new Material(GetShader(TRANSPARENT_OPAQUE_PART_SHADER_PATH));
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
            #endif
        }
        
        private static Material CreateOpaqueMaterial(Texture2D mainTexture,
            Color tintColor,
            Texture2D shadowTexture)
        {
            #if RTX_ON
            Material material = new Material(GetShader(REALTOON_DEFAULT_SHADER_PATH));
            material.mainTexture = mainTexture;
            return material;
            #else
            Material material = new Material(GetShader(OPAQUE_SHADER_PATH));
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
            #endif
        }
    }
}