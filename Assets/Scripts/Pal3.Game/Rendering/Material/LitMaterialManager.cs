// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Game.Rendering.Material
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using Core.Primitives;
    using Engine.Core.Abstraction;
    using Engine.Logging;
    using UnityEngine;
    using UnityEngine.Rendering;
    using Color = Core.Primitives.Color;

    /// <summary>
    /// Lit material factory for generating materials
    /// to be used when lighting and shadow are enabled
    /// </summary>
    public sealed class LitMaterialManager : MaterialManagerBase, IMaterialManager
    {
        // Toon material uniforms
        private static readonly int BlendSrcFactorPropertyId = Shader.PropertyToID("_BlendSrcFactor");
        private static readonly int BlendDstFactorPropertyId = Shader.PropertyToID("_BlendDstFactor");
        private static readonly int CutoutPropertyId = Shader.PropertyToID("_Cutout");
        private static readonly int LightIntensityPropertyId = Shader.PropertyToID("_LightIntensity");
        private static readonly int OpacityPropertyId = Shader.PropertyToID("_Opacity");
        private static readonly int EnvironmentalLightingIntensityPropertyId = Shader.PropertyToID("_EnvironmentalLightingIntensity");

        private readonly IMaterial _toonOpaqueMaterial;
        private readonly IMaterial _toonTransparentMaterial;

        private const string OPAQUE_MATERIAL_SHADER_NAME = "RealToon/Version 5/Default/Default";
        private const string TRANSPARENT_MATERIAL_SHADER_NAME = "RealToon/Version 5/Default/Fade Transparency";

        private const int OPAQUE_MATERIAL_POOL_SIZE = 3500;
        private const int TRANSPARENT_MATERIAL_POOL_SIZE = 500;

        private readonly Stack<IMaterial> _opaqueMaterialPool = new (OPAQUE_MATERIAL_POOL_SIZE);
        private readonly Stack<IMaterial> _transparentMaterialPool = new (TRANSPARENT_MATERIAL_POOL_SIZE);

        private readonly float _opaqueMaterialCutoutPropertyDefaultValue;
        private readonly float _opaqueMaterialLightIntensityPropertyDefaultValue;
        private readonly float _opaqueMaterialEnvironmentalLightingIntensityPropertyDefaultValue;

        private readonly int _transparentMaterialBlendSrcFactorPropertyDefaultValue;
        private readonly int _transparentMaterialBlendDstFactorPropertyDefaultValue;
        private readonly float _transparentMaterialOpacityPropertyDefaultValue;
        private readonly float _transparentMaterialLightIntensityPropertyDefaultValue;
        private readonly float _transparentMaterialEnvironmentalLightingIntensityPropertyDefaultValue;

        private bool _isMaterialPoolAllocated = false;

        public LitMaterialManager(
            IMaterialFactory materialFactory,
            IMaterial toonOpaqueMaterial,
            IMaterial toonTransparentMaterial) : base (materialFactory)
        {
            _toonOpaqueMaterial = toonOpaqueMaterial;
            _toonTransparentMaterial = toonTransparentMaterial;

            _opaqueMaterialCutoutPropertyDefaultValue =
                _toonOpaqueMaterial.GetFloat(CutoutPropertyId);
            _opaqueMaterialLightIntensityPropertyDefaultValue =
                _toonOpaqueMaterial.GetFloat(LightIntensityPropertyId);
            _opaqueMaterialEnvironmentalLightingIntensityPropertyDefaultValue =
                _toonOpaqueMaterial.GetFloat(EnvironmentalLightingIntensityPropertyId);

            _transparentMaterialBlendSrcFactorPropertyDefaultValue =
                _toonTransparentMaterial.GetInt(BlendSrcFactorPropertyId);
            _transparentMaterialBlendDstFactorPropertyDefaultValue =
                _toonTransparentMaterial.GetInt(BlendDstFactorPropertyId);
            _transparentMaterialOpacityPropertyDefaultValue = _toonTransparentMaterial.GetFloat(OpacityPropertyId);
            _transparentMaterialLightIntensityPropertyDefaultValue =
                _toonTransparentMaterial.GetFloat(LightIntensityPropertyId);
            _transparentMaterialEnvironmentalLightingIntensityPropertyDefaultValue =
                _toonTransparentMaterial.GetFloat(EnvironmentalLightingIntensityPropertyId);
        }

        public void AllocateMaterialPool()
        {
            if (_isMaterialPoolAllocated) return;

            Stopwatch timer = Stopwatch.StartNew();

            for (var i = 0; i < OPAQUE_MATERIAL_POOL_SIZE; i++)
            {
                _opaqueMaterialPool.Push(MaterialFactory.CreateMaterialFrom(_toonOpaqueMaterial));
            }

            for (var i = 0; i < TRANSPARENT_MATERIAL_POOL_SIZE; i++)
            {
                _transparentMaterialPool.Push(MaterialFactory.CreateMaterialFrom(_toonTransparentMaterial));
            }

            _isMaterialPoolAllocated = true;

            EngineLogger.Log($"Material pool allocated in {timer.ElapsedMilliseconds} ms");
        }

        public void DeallocateMaterialPool()
        {
            Stopwatch timer = Stopwatch.StartNew();

            while (_opaqueMaterialPool.Count > 0)
            {
                _opaqueMaterialPool.Pop().Destroy();
            }

            while (_transparentMaterialPool.Count > 0)
            {
                _transparentMaterialPool.Pop().Destroy();
            }

            _isMaterialPoolAllocated = false;

            EngineLogger.Log($"Material pool de-allocated in {timer.ElapsedMilliseconds} ms");
        }

        public MaterialShaderType ShaderType => MaterialShaderType.Lit;

        /// <inheritdoc/>
        public IMaterial[] CreateStandardMaterials(
            RendererType rendererType,
            (string name, ITexture2D texture) mainTexture,
            (string name, ITexture2D texture) shadowTexture,
            Color tintColor,
            GameBoxBlendFlag blendFlag)
        {
            IMaterial[] materials = null;

            if (blendFlag is GameBoxBlendFlag.AlphaBlend or GameBoxBlendFlag.InvertColorBlend)
            {
                #if PAL3
                // These are the texture/materials that have to be transparent in the game
                // m23-3-05.tga => transparent doom like structure in PAL3 scene M23-3
                // Q08QN10-05.tga => transparent coffin in PAL3 scene Q08-QN10
                // m25-2-05.tga => transparent roof in PAL3 scene M25-2
                // TODO: Add more if needed, by default we use opaque material
                bool useTransparentMaterial = blendFlag == GameBoxBlendFlag.InvertColorBlend ||
                        mainTexture.name.Equals("m23-3-05.tga", StringComparison.OrdinalIgnoreCase) ||
                        mainTexture.name.Equals("Q08QN10-05.tga", StringComparison.OrdinalIgnoreCase) ||
                        mainTexture.name.Equals("m25-2-05.tga", StringComparison.OrdinalIgnoreCase);
                #elif PAL3A
                bool useTransparentMaterial = blendFlag == GameBoxBlendFlag.InvertColorBlend;
                #endif

                // For CVD renderer, we always use transparent material if blendFlag is
                // InvertColorBlend or AlphaBlend
                if (rendererType == RendererType.Cvd) useTransparentMaterial = true;

                materials = new IMaterial[1];
                materials[0] = useTransparentMaterial
                    ? CreateBaseTransparentMaterial(mainTexture, shadowTexture)
                    : CreateBaseOpaqueMaterial(mainTexture, shadowTexture);

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
                materials = new IMaterial[1];
                materials[0] = CreateBaseOpaqueMaterial(mainTexture, shadowTexture);
            }

            if (materials != null && rendererType == RendererType.Mv3)
            {
                // This is to make the shadow on actor lighter
                materials[0].SetFloat(LightIntensityPropertyId, -0.5f);
            }

            return materials;
        }

        public void UpdateMaterial(IMaterial material, ITexture2D newMainTexture, GameBoxBlendFlag blendFlag)
        {
            if (newMainTexture != null)
            {
                material.SetMainTexture(newMainTexture);
            }

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
        public IMaterial CreateWaterMaterial(
            (string name, ITexture2D texture) mainTexture,
            (string name, ITexture2D texture) shadowTexture,
            float opacity,
            GameBoxBlendFlag blendFlag)
        {
            if (blendFlag == GameBoxBlendFlag.Opaque)
            {
                opacity = 1.0f;
            }

            // This is a tweak to make the water surface look better
            // when lit materials are used
            opacity = opacity < 0.8f ? 0.8f : opacity;

            // No need to use transparent material if alpha is close to 1
            bool useTransparentMaterial = opacity < 0.9f;

            if (useTransparentMaterial)
            {
                IMaterial material = CreateBaseTransparentMaterial(mainTexture, shadowTexture);
                material.SetFloat(OpacityPropertyId, opacity);
                material.SetFloat(EnvironmentalLightingIntensityPropertyId, 0.3f);
                return material;
            }
            else
            {
                IMaterial material = CreateBaseOpaqueMaterial(mainTexture, shadowTexture);
                material.SetFloat(EnvironmentalLightingIntensityPropertyId, 0.5f);
                return material;
            }
        }

        private IMaterial CreateBaseTransparentMaterial((string name, ITexture2D texture) mainTexture,
            (string name, ITexture2D texture) shadowTexture)
        {
            IMaterial material;
            if (_transparentMaterialPool.Count > 0)
            {
                material = _transparentMaterialPool.Pop();
                // Reset material properties
                material.SetInt(BlendSrcFactorPropertyId,
                    _transparentMaterialBlendSrcFactorPropertyDefaultValue);
                material.SetInt(BlendDstFactorPropertyId,
                    _transparentMaterialBlendDstFactorPropertyDefaultValue);
                material.SetFloat(OpacityPropertyId,
                    _transparentMaterialOpacityPropertyDefaultValue);
                material.SetFloat(LightIntensityPropertyId,
                    _transparentMaterialLightIntensityPropertyDefaultValue);
                material.SetFloat(EnvironmentalLightingIntensityPropertyId,
                    _transparentMaterialEnvironmentalLightingIntensityPropertyDefaultValue);
            }
            else
            {
                // In case we run out of transparent materials
                material = MaterialFactory.CreateMaterialFrom(_toonTransparentMaterial);
            }

            if (mainTexture.texture != null)
            {
                material.SetMainTexture(mainTexture.texture);
            }

            return material;
        }

        private IMaterial CreateBaseOpaqueMaterial((string name, ITexture2D texture) mainTexture,
            (string name, ITexture2D texture) shadowTexture)
        {
            IMaterial material;
            if (_opaqueMaterialPool.Count > 0)
            {
                material = _opaqueMaterialPool.Pop();
                // Reset material properties
                material.SetFloat(CutoutPropertyId,
                    _opaqueMaterialCutoutPropertyDefaultValue);
                material.SetFloat(LightIntensityPropertyId,
                    _opaqueMaterialLightIntensityPropertyDefaultValue);
                material.SetFloat(EnvironmentalLightingIntensityPropertyId,
                    _opaqueMaterialEnvironmentalLightingIntensityPropertyDefaultValue);
            }
            else
            {
                // In case we run out of opaque materials
                material = MaterialFactory.CreateMaterialFrom(_toonOpaqueMaterial);
            }

            if (mainTexture.texture != null)
            {
                material.SetMainTexture(mainTexture.texture);
            }

            return material;
        }

        protected override void ReturnToPool(IMaterial material)
        {
            switch (material.ShaderName)
            {
                case TRANSPARENT_MATERIAL_SHADER_NAME:
                    material.SetMainTexture(null);
                    _transparentMaterialPool.Push(material);
                    break;
                case OPAQUE_MATERIAL_SHADER_NAME:
                    material.SetMainTexture(null);
                    _opaqueMaterialPool.Push(material);
                    break;
                default:
                    return;
            }
        }
    }
}