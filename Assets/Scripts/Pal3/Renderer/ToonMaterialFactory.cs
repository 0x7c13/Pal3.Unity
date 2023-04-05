// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Renderer
{
    using Core.GameBox;
    using UnityEngine;

    public class ToonMaterialFactory : MaterialFactoryBase, IMaterialFactory
    {
        // Toon material uniforms
        private static readonly int CutoutPropertyId = Shader.PropertyToID("_Cutout");
        private readonly int _lightIntensityPropertyId = Shader.PropertyToID("_LightIntensity");

        private readonly Material _toonMaterial;
        private readonly Shader _toonShader;

        public ToonMaterialFactory()
        {
            _toonMaterial = Resources.Load<Material>("Materials/Toon");
            _toonShader = _toonMaterial.shader;
        }

        /// <inheritdoc/>
        public Material CreateWaterMaterial(Texture2D mainTexture,
            Texture2D shadowTexture,
            float alpha)
        {
            Material material = new Material(_toonShader);
            material.CopyPropertiesFromMaterial(_toonMaterial);
            material.mainTexture = mainTexture;
            return material;
        }

        /// <inheritdoc/>
        public Material[] CreateStandardMaterials(
            RendererType rendererType,
            Texture2D mainTexture,
            Texture2D shadowTexture,
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

        private Material CreateTransparentMaterial(Texture2D mainTexture)
        {
            Material material = new Material(_toonShader);
            material.CopyPropertiesFromMaterial(_toonMaterial);
            material.SetFloat(CutoutPropertyId, 0.3f);
            material.mainTexture = mainTexture;
            return material;
        }

        private Material CreateOpaqueMaterial(Texture2D mainTexture)
        {
            Material material = new Material(_toonShader);
            material.CopyPropertiesFromMaterial(_toonMaterial);
            material.mainTexture = mainTexture;
            return material;
        }
        
        public Material CreateBoneGizmoMaterial()
        {
            return null;
        }
    }
}