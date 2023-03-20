// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Renderer
{
    using Core.GameBox;
    using UnityEngine;

    /// <summary>
    /// Lit material factory for generating materials
    /// to be used when lighting and shadow are enabled
    /// </summary>
    public class LitMaterialFactory : MaterialFactoryBase, IMaterialFactory
    {
        // Toon material uniforms
        private static readonly int CutoutPropertyId = Shader.PropertyToID("_Cutout");
        private readonly int _lightIntensityPropertyId = Shader.PropertyToID("_LightIntensity");

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
                materials = new Material[1];
                materials[0] = CreateTransparentMaterial(mainTexture);
            }
            else if (blendFlag == GameBoxBlendFlag.Opaque)
            {
                materials = new Material[1];
                materials[0] = CreateOpaqueMaterial(mainTexture);
            }

            if (materials != null && rendererType == RendererType.Mv3)
            {
                // This is to make the shadow on actor lighter
                materials[0].SetFloat(_lightIntensityPropertyId, -0.5f);
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

        private Material CreateTransparentMaterial((string name, Texture2D texture) mainTexture)
        {
            Material material;

            // These are the texture/materials that have to be transparent in the game
            // m23-3-05.tga => transparent doom like structure in PAL3 scene M23-3
            // Q08QN10-05.tga => transparent coffin in PAL3 scene Q08-QN10
            // TODO: Add more if needed
            #if PAL3
            if (mainTexture.name is "m23-3-05.tga" or "Q08QN10-05.tga")
            #elif PAL3A
            if (false)
            #endif
            {
                material = new Material(_toonTransparentShader);
                material.CopyPropertiesFromMaterial(_toonTransparentMaterial);
            }
            else // Other transparent materials can be rendered as opaque with a cutout
                 // Since toon transparent material does not support shadow casting, which
                 // does not look good in most cases
            {
                material = new Material(_toonDefaultShader);
                material.CopyPropertiesFromMaterial(_toonDefaultMaterial);
                material.SetFloat(CutoutPropertyId, 0.3f);
            }

            material.mainTexture = mainTexture.texture;
            return material;
        }

        private Material CreateOpaqueMaterial((string name, Texture2D texture) mainTexture)
        {
            Material material = new Material(_toonDefaultShader);
            material.CopyPropertiesFromMaterial(_toonDefaultMaterial);
            material.mainTexture = mainTexture.texture;
            return material;
        }
    }
}