// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2025, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Game.Rendering.Material
{
    using System.Collections.Generic;
    using System.Diagnostics;
    using Core.Primitives;
    using Engine.Core.Abstraction;
    using Engine.Logging;
    using Engine.Services;
    using UnityEngine.Rendering;
    using Color = Core.Primitives.Color;

    /// <summary>
    /// Unlit material factory for generating materials that have similar
    /// look and feel as the original game
    /// </summary>
    public sealed class UnlitMaterialManager : MaterialManagerBase, IMaterialManager
    {
        // Pal3 unlit shaders
        private const string OPAQUE_SHADER_PATH = "Pal3/Opaque";
        private const string TRANSPARENT_SHADER_PATH = "Pal3/Transparent";
        private const string TRANSPARENT_OPAQUE_PART_SHADER_PATH = "Pal3/TransparentOpaquePart";
        private const string WATER_SHADER_PATH = "Pal3/Water";

        private const string OPAQUE_SHADER_NAME = "Pal3/Opaque";
        private const string TRANSPARENT_SHADER_NAME = "Pal3/Transparent";
        private const string TRANSPARENT_OPAQUE_PART_SHADER_NAME = "Pal3/TransparentOpaquePart";
        private const string WATER_SHADER_NAME = "Pal3/Water";

        // Water material uniforms
        private static readonly int WaterAlphaPropId = ShaderUtility.GetPropertyIdByName("_Alpha");
        private static readonly int WaterHasShadowTexPropId = ShaderUtility.GetPropertyIdByName("_HasShadowTex");

        // Standard material uniforms for Pal3 unlit shaders
        private static readonly int BlendSrcFactorPropertyId = ShaderUtility.GetPropertyIdByName("_BlendSrcFactor");
        private static readonly int BlendDstFactorPropertyId = ShaderUtility.GetPropertyIdByName("_BlendDstFactor");
        private static readonly int TintColorPropertyId = ShaderUtility.GetPropertyIdByName("_TintColor");
        private static readonly int TransparentThresholdPropertyId = ShaderUtility.GetPropertyIdByName("_Threshold");
        private static readonly int HasShadowTexturePropertyId = ShaderUtility.GetPropertyIdByName("_HasShadowTex");
        private static readonly int ShadowTexturePropertyId = ShaderUtility.GetPropertyIdByName("_ShadowTex");

        private const float DEFAULT_TRANSPARENT_THRESHOLD = 0.9f;

        public MaterialShaderType ShaderType => MaterialShaderType.Unlit;

        private readonly IMaterial _waterMaterial;
        private readonly IMaterial _transparentMaterial;
        private readonly IMaterial _transparentOpaquePartMaterial;
        private readonly IMaterial _opaqueMaterial;

        private const int WATER_MATERIAL_POOL_SIZE = 100;
        private const int TRANSPARENT_MATERIAL_POOL_SIZE = 1500;
        private const int TRANSPARENT_OPAQUE_PART_MATERIAL_POOL_SIZE = 1500;
        private const int OPAQUE_MATERIAL_POOL_SIZE = 2000;

        private readonly Stack<IMaterial> _waterMaterialPool = new (WATER_MATERIAL_POOL_SIZE);
        private readonly Stack<IMaterial> _transparentMaterialPool = new (TRANSPARENT_MATERIAL_POOL_SIZE);
        private readonly Stack<IMaterial> _transparentOpaquePartMaterialPool = new (TRANSPARENT_OPAQUE_PART_MATERIAL_POOL_SIZE);
        private readonly Stack<IMaterial> _opaqueMaterialPool = new (OPAQUE_MATERIAL_POOL_SIZE);

        private bool _isMaterialPoolAllocated = false;

        public UnlitMaterialManager(IMaterialFactory materialFactory) : base(materialFactory)
        {
            _waterMaterial = MaterialFactory.CreateMaterial(WATER_SHADER_PATH);
            _transparentMaterial = MaterialFactory.CreateMaterial(TRANSPARENT_SHADER_PATH);
            _transparentOpaquePartMaterial = MaterialFactory.CreateMaterial(TRANSPARENT_OPAQUE_PART_SHADER_PATH);
            _opaqueMaterial = MaterialFactory.CreateMaterial(OPAQUE_SHADER_PATH);
        }

        public void AllocateMaterialPool()
        {
            if (_isMaterialPoolAllocated) return;

            Stopwatch timer = Stopwatch.StartNew();

            for (int i = 0; i < WATER_MATERIAL_POOL_SIZE; i++)
            {
                _waterMaterialPool.Push(MaterialFactory.CreateMaterialFrom(_waterMaterial));
            }

            for (int i = 0; i < TRANSPARENT_MATERIAL_POOL_SIZE; i++)
            {
                _transparentMaterialPool.Push(MaterialFactory.CreateMaterialFrom(_transparentMaterial));
            }

            for (int i = 0; i < TRANSPARENT_OPAQUE_PART_MATERIAL_POOL_SIZE; i++)
            {
                _transparentOpaquePartMaterialPool.Push(MaterialFactory.CreateMaterialFrom(_transparentOpaquePartMaterial));
            }

            for (int i = 0; i < OPAQUE_MATERIAL_POOL_SIZE; i++)
            {
                _opaqueMaterialPool.Push(MaterialFactory.CreateMaterialFrom(_opaqueMaterial));
            }

            EngineLogger.Log($"Material pool allocated in {timer.ElapsedMilliseconds} ms");
        }

        public void DeallocateMaterialPool()
        {
            Stopwatch timer = Stopwatch.StartNew();

            while (_waterMaterialPool.Count > 0)
            {
                _waterMaterialPool.Pop().Destroy();
            }

            while (_transparentMaterialPool.Count > 0)
            {
                _transparentMaterialPool.Pop().Destroy();
            }

            while (_transparentOpaquePartMaterialPool.Count > 0)
            {
                _transparentOpaquePartMaterialPool.Pop().Destroy();
            }

            while (_opaqueMaterialPool.Count > 0)
            {
                _opaqueMaterialPool.Pop().Destroy();
            }

            _isMaterialPoolAllocated = false;

            EngineLogger.Log($"Material pool de-allocated in {timer.ElapsedMilliseconds} ms");
        }

        /// <inheritdoc/>
        public IMaterial[] CreateStandardMaterials(
            RendererType rendererType,
            (string name, ITexture2D texture) mainTexture,
            (string name, ITexture2D texture) shadowTexture,
            Color tintColor,
            GameBoxBlendFlag blendFlag)
        {
            IMaterial[] materials = null;

            float transparentThreshold = DEFAULT_TRANSPARENT_THRESHOLD;

            if (shadowTexture.texture == null && rendererType == RendererType.Pol)
            {
                transparentThreshold = 1.0f;
            }

            if (blendFlag is GameBoxBlendFlag.AlphaBlend or GameBoxBlendFlag.InvertColorBlend)
            {
                materials = new IMaterial[2];
                materials[0] = CreateTransparentOpaquePartMaterial(mainTexture,
                    shadowTexture,
                    tintColor,
                    transparentThreshold);
                materials[1] = CreateTransparentMaterial(mainTexture,
                    shadowTexture,
                    tintColor,
                    transparentThreshold);

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
                materials = new IMaterial[1];
                materials[0] = CreateOpaqueMaterial(mainTexture, shadowTexture, tintColor);
            }

            return materials;
        }

        public void UpdateMaterial(IMaterial material,
            ITexture2D newMainTexture,
            GameBoxBlendFlag blendFlag)
        {
            if (newMainTexture != null)
            {
                material.SetMainTexture(newMainTexture);
            }

            if (blendFlag is GameBoxBlendFlag.AlphaBlend or GameBoxBlendFlag.InvertColorBlend)
            {
                material.SetFloat(TransparentThresholdPropertyId, DEFAULT_TRANSPARENT_THRESHOLD);
            }
            else if (blendFlag == GameBoxBlendFlag.Opaque)
            {
                material.SetFloat(TransparentThresholdPropertyId, 0f);
            }
        }

        /// <inheritdoc/>
        public IMaterial CreateWaterMaterial(
            (string name, ITexture2D texture) mainTexture,
            (string name, ITexture2D texture) shadowTexture,
            float opacity,
            GameBoxBlendFlag blendFlag)
        {
            IMaterial material;
            if (_waterMaterialPool.Count > 0)
            {
                material = _waterMaterialPool.Pop();
            }
            else
            {
                material = MaterialFactory.CreateMaterialFrom(_waterMaterial);
            }

            if (mainTexture.texture != null)
            {
                material.SetMainTexture(mainTexture.texture);
            }

            material.SetFloat(WaterAlphaPropId, opacity);

            if (shadowTexture.texture != null)
            {
                material.SetFloat(WaterHasShadowTexPropId, 1.0f);
                material.SetTexture(ShadowTexturePropertyId, shadowTexture.texture);
            }
            else
            {
                material.SetFloat(WaterHasShadowTexPropId, 0.5f);
                material.SetTexture(ShadowTexturePropertyId, null);
            }
            return material;
        }

        private IMaterial CreateTransparentMaterial(
            (string name, ITexture2D texture) mainTexture,
            (string name, ITexture2D texture) shadowTexture,
            Color tintColor,
            float transparentThreshold)
        {
            IMaterial material = _transparentMaterialPool.Count > 0 ?
                _transparentMaterialPool.Pop() :
                MaterialFactory.CreateMaterialFrom(_transparentMaterial);

            if (mainTexture.texture != null)
            {
                material.SetMainTexture(mainTexture.texture);
            }

            material.SetColor(TintColorPropertyId, tintColor);
            material.SetFloat(TransparentThresholdPropertyId, transparentThreshold);

            if (shadowTexture.texture != null)
            {
                material.SetFloat(HasShadowTexturePropertyId, 1.0f);
                material.SetTexture(ShadowTexturePropertyId, shadowTexture.texture);
            }
            else
            {
                material.SetFloat(HasShadowTexturePropertyId, 0.0f);
                material.SetTexture(ShadowTexturePropertyId, null);
            }

            return material;
        }

        private IMaterial CreateTransparentOpaquePartMaterial(
            (string name, ITexture2D texture) mainTexture,
            (string name, ITexture2D texture) shadowTexture,
            Color tintColor,
            float transparentThreshold)
        {
            IMaterial material = _transparentOpaquePartMaterialPool.Count > 0 ?
                _transparentOpaquePartMaterialPool.Pop() :
                MaterialFactory.CreateMaterialFrom(_transparentOpaquePartMaterial);

            if (mainTexture.texture != null)
            {
                material.SetMainTexture(mainTexture.texture);
            }

            material.SetColor(TintColorPropertyId, tintColor);
            material.SetFloat(TransparentThresholdPropertyId, transparentThreshold);

            if (shadowTexture.texture != null)
            {
                material.SetFloat(HasShadowTexturePropertyId, 1.0f);
                material.SetTexture(ShadowTexturePropertyId, shadowTexture.texture);
            }
            else
            {
                material.SetFloat(HasShadowTexturePropertyId, 0.0f);
                material.SetTexture(ShadowTexturePropertyId, null);
            }

            return material;
        }

        private IMaterial CreateOpaqueMaterial(
            (string name, ITexture2D texture) mainTexture,
            (string name, ITexture2D texture) shadowTexture,
            Color tintColor)
        {
            IMaterial material = _opaqueMaterialPool.Count > 0 ?
                _opaqueMaterialPool.Pop() :
                MaterialFactory.CreateMaterialFrom(_opaqueMaterial);

            if (mainTexture.texture != null)
            {
                material.SetMainTexture(mainTexture.texture);
            }

            material.SetColor(TintColorPropertyId, tintColor);

            if (shadowTexture.texture != null)
            {
                material.SetFloat(HasShadowTexturePropertyId, 1.0f);
                material.SetTexture(ShadowTexturePropertyId, shadowTexture.texture);
            }
            else
            {
                material.SetFloat(HasShadowTexturePropertyId, 0.0f);
                material.SetTexture(ShadowTexturePropertyId, null);
            }

            return material;
        }

        protected override void ReturnToPool(IMaterial material)
        {
            switch (material.ShaderName)
            {
                case WATER_SHADER_NAME:
                    material.SetMainTexture(null);
                    material.SetTexture(ShadowTexturePropertyId, null);
                    _waterMaterialPool.Push(material);
                    break;
                case TRANSPARENT_SHADER_NAME:
                    material.SetMainTexture(null);
                    material.SetTexture(ShadowTexturePropertyId, null);
                    _transparentMaterialPool.Push(material);
                    break;
                case TRANSPARENT_OPAQUE_PART_SHADER_NAME:
                    material.SetMainTexture(null);
                    material.SetTexture(ShadowTexturePropertyId, null);
                    _transparentOpaquePartMaterialPool.Push(material);
                    break;
                case OPAQUE_SHADER_NAME:
                    material.SetMainTexture(null);
                    material.SetTexture(ShadowTexturePropertyId, null);
                    _opaqueMaterialPool.Push(material);
                    break;
                default:
                    return;
            }
        }
    }
}