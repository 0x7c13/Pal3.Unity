// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Renderer
{
    using Core.GameBox;
    using UnityEngine;
    using UnityEngine.Rendering;

    /// <summary>
    /// Lit material factory for generating materials
    /// to be used when lighting and shadow are enabled
    /// </summary>
    public class LitMaterialFactory : MaterialFactoryBase, IMaterialFactory
    {
        // Toon material uniforms
        private static readonly int BlendSrcFactorPropertyId = Shader.PropertyToID("_BlendSrcFactor");
        private static readonly int BlendDstFactorPropertyId = Shader.PropertyToID("_BlendDstFactor");
        private static readonly int CutoutPropertyId = Shader.PropertyToID("_Cutout");
        private static readonly int LightIntensityPropertyId = Shader.PropertyToID("_LightIntensity");
        private static readonly int OpacityPropertyId = Shader.PropertyToID("_Opacity");
        private static readonly int EnvironmentalLightingIntensityPropertyId = Shader.PropertyToID("_EnvironmentalLightingIntensity");

        private readonly Material _toonDefaultMaterial;
        private readonly Material _toonTransparentMaterial;
        private readonly Shader _toonDefaultShader;
        private readonly Shader _toonTransparentShader;

        public LitMaterialFactory(
            Material toonDefaultMaterial,
            Material toonTransparentMaterial)
        {
            _toonDefaultMaterial = toonDefaultMaterial;
            _toonTransparentMaterial = toonTransparentMaterial;

            _toonDefaultShader = toonDefaultMaterial.shader;
            _toonTransparentShader = toonTransparentMaterial.shader;
        }

        /// <inheritdoc/>
        public Material[] CreateStandardMaterials(
            RendererType rendererType,
            (string name, Texture2D texture) mainTexture,
            (string name, Texture2D texture) shadowTexture,
            Color tintColor,
            GameBoxBlendFlag blendFlag)
        {
            Material[] materials = null;

            if (blendFlag is GameBoxBlendFlag.AlphaBlend or GameBoxBlendFlag.InvertColorBlend)
            {
                #if PAL3
                // These are the texture/materials that have to be transparent in the game
                // m23-3-05.tga => transparent doom like structure in PAL3 scene M23-3
                // Q08QN10-05.tga => transparent coffin in PAL3 scene Q08-QN10
                // TODO: Add more if needed
                bool useTransparentMaterial = mainTexture.name is "m23-3-05.tga" or "Q08QN10-05.tga" ||
                                              blendFlag == GameBoxBlendFlag.InvertColorBlend;
                #elif PAL3A
                bool useTransparentMaterial = blendFlag == GameBoxBlendFlag.InvertColorBlend;
                #endif

                materials = new Material[1];
                materials[0] = useTransparentMaterial
                    ? CreateBaseTransparentMaterial(mainTexture)
                    : CreateBaseOpaqueMaterial(mainTexture);

                if (!useTransparentMaterial)
                {
                    materials[0].SetFloat(CutoutPropertyId, 0.3f);
                }

                if (blendFlag == GameBoxBlendFlag.InvertColorBlend)
                {
                    materials[0].SetInt(BlendSrcFactorPropertyId, (int)BlendMode.SrcAlpha);
                    materials[0].SetInt(BlendDstFactorPropertyId, (int)BlendMode.One);
                }
            }
            else if (blendFlag == GameBoxBlendFlag.Opaque)
            {
                materials = new Material[1];
                materials[0] = CreateBaseOpaqueMaterial(mainTexture);
            }

            if (materials != null && rendererType == RendererType.Mv3)
            {
                // This is to make the shadow on actor lighter
                materials[0].SetFloat(LightIntensityPropertyId, -0.5f);
            }

            return materials;
        }

        public void UpdateMaterial(Material material, Texture2D newMainTexture, GameBoxBlendFlag blendFlag)
        {
            material.mainTexture = newMainTexture;

            if (blendFlag is GameBoxBlendFlag.AlphaBlend or GameBoxBlendFlag.InvertColorBlend)
            {
                material.SetFloat(CutoutPropertyId, 0.3f);
            }
            else if (blendFlag == GameBoxBlendFlag.Opaque)
            {
                material.SetFloat(CutoutPropertyId, 0f);
            }
        }

        /// <inheritdoc/>
        public Material CreateWaterMaterial(
            (string name, Texture2D texture) mainTexture,
            (string name, Texture2D texture) shadowTexture,
            float alpha,
            GameBoxBlendFlag blendFlag)
        {
            if (blendFlag == GameBoxBlendFlag.Opaque)
            {
                alpha = 1.0f;
            }

            // This is a tweak to make the water surface look better
            // when lit materials are used
            alpha = alpha < 0.8f ? 0.8f : alpha;

            // No need to use transparent material if alpha is close to 1
            bool useTransparentMaterial = alpha < 0.9f;

            #if PAL3A
            // There are water surfaces that are better to be opaque
            // but have alpha values less than 1 and blend flag set to
            // alpha blend. This is a hack to fix that.
            if (mainTexture.name == "w0001.tga" &&
                shadowTexture.name is "^L_Object02.bmp")
            {
                useTransparentMaterial = false;
            }
            #endif

            if (useTransparentMaterial)
            {
                Material material = CreateBaseTransparentMaterial(mainTexture);
                material.SetFloat(OpacityPropertyId, alpha);
                material.SetFloat(EnvironmentalLightingIntensityPropertyId, 0.3f);
                return material;
            }
            else
            {
                Material material = CreateBaseOpaqueMaterial(mainTexture);
                material.SetFloat(EnvironmentalLightingIntensityPropertyId, 0.5f);
                return material;
            }
        }

        private Material CreateBaseTransparentMaterial((string name, Texture2D texture) mainTexture)
        {
            Material material = new Material(_toonTransparentShader);
            material.CopyPropertiesFromMaterial(_toonTransparentMaterial);
            material.mainTexture = mainTexture.texture;
            return material;
        }

        private Material CreateBaseOpaqueMaterial((string name, Texture2D texture) mainTexture)
        {
            Material material = new Material(_toonDefaultShader);
            material.CopyPropertiesFromMaterial(_toonDefaultMaterial);
            material.mainTexture = mainTexture.texture;
            return material;
        }
    }
}