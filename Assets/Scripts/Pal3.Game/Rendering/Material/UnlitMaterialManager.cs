// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2025, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Game.Rendering.Material
{
    using System;
    using System.Collections.Generic;
    using Core.Primitives;
    using Engine.Core.Abstraction;
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


        // Water material uniforms
        private static readonly int WaterAlphaPropId = ShaderUtility.GetPropertyIdByName("_Alpha");

        // Standard material uniforms for Pal3 unlit shaders
        private static readonly int BlendSrcFactorPropertyId = ShaderUtility.GetPropertyIdByName("_BlendSrcFactor");
        private static readonly int BlendDstFactorPropertyId = ShaderUtility.GetPropertyIdByName("_BlendDstFactor");
        private static readonly int TintColorPropertyId = ShaderUtility.GetPropertyIdByName("_TintColor");
        private static readonly int TransparentThresholdPropertyId = ShaderUtility.GetPropertyIdByName("_Threshold");
        private static readonly int HasShadowTexturePropertyId = ShaderUtility.GetPropertyIdByName("_HasShadowTex");
        private static readonly int LightmapScalePropertyId = ShaderUtility.GetPropertyIdByName("_LightmapScale");
        private static readonly int ShadowTexturePropertyId = ShaderUtility.GetPropertyIdByName("_ShadowTex");

        private const float DefaultAlphaCutoff = 0.9f;
        private const float PolAlphaCutoff = 245f / 255f;
        private const float DisabledAlphaCutoff = 2f;
        private const float DefaultLightmapScale = 1f / 0.6f;
        private const float PolLightmapScale = 2f;

        public MaterialShaderType ShaderType => MaterialShaderType.Unlit;

        private readonly IMaterial _waterMaterial;
        private readonly IMaterial _transparentMaterial;
        private readonly IMaterial _transparentOpaquePartMaterial;
        private readonly IMaterial _opaqueMaterial;

        private const int WATER_MATERIAL_POOL_SIZE = 100;
        private const int TRANSPARENT_MATERIAL_POOL_SIZE = 1500;
        private const int TRANSPARENT_OPAQUE_PART_MATERIAL_POOL_SIZE = 1500;
        private const int OPAQUE_MATERIAL_POOL_SIZE = 2000;

        private readonly Stack<IMaterial> _waterMaterialPool = new();
        private readonly Stack<IMaterial> _transparentMaterialPool = new();
        private readonly Stack<IMaterial> _transparentOpaquePartMaterialPool = new();
        private readonly Stack<IMaterial> _opaqueMaterialPool = new();

        private bool _disposed;

        public UnlitMaterialManager(IMaterialFactory materialFactory) : base(materialFactory)
        {
            _waterMaterial = MaterialFactory.CreateMaterial(WATER_SHADER_PATH);
            _transparentMaterial = MaterialFactory.CreateMaterial(TRANSPARENT_SHADER_PATH);
            _transparentOpaquePartMaterial = MaterialFactory.CreateMaterial(TRANSPARENT_OPAQUE_PART_SHADER_PATH);
            _opaqueMaterial = MaterialFactory.CreateMaterial(OPAQUE_SHADER_PATH);
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            DisposePool(_waterMaterialPool);
            DisposePool(_transparentMaterialPool);
            DisposePool(_transparentOpaquePartMaterialPool);
            DisposePool(_opaqueMaterialPool);
            _waterMaterial.Destroy();
            _transparentMaterial.Destroy();
            _transparentOpaquePartMaterial.Destroy();
            _opaqueMaterial.Destroy();
        }

        private static void DisposePool(Stack<IMaterial> pool)
        {
            while (pool.TryPop(out IMaterial material))
            {
                material.Destroy();
            }
        }

        /// <inheritdoc/>
        public IMaterial[] CreateStandardMaterials(
            RendererType rendererType,
            (string name, ITexture2D texture) mainTexture,
            (string name, ITexture2D texture) shadowTexture,
            Color tintColor,
            GameBoxBlendFlag blendFlag)
        {
            float lightmapScale = rendererType == RendererType.Pol ? PolLightmapScale : DefaultLightmapScale;
            switch (blendFlag)
            {
                case GameBoxBlendFlag.Opaque:
                    return new[] { CreateMeshMaterial(_opaqueMaterialPool, _opaqueMaterial) };
                case GameBoxBlendFlag.AlphaBlend:
                {
                    float cutoff = rendererType == RendererType.Pol ? PolAlphaCutoff : DefaultAlphaCutoff;
                    IMaterial depthWriting = CreateMeshMaterial(_transparentOpaquePartMaterialPool,
                        _transparentOpaquePartMaterial);
                    depthWriting.SetFloat(TransparentThresholdPropertyId, cutoff);
                    return new[] { depthWriting, CreateBlendedMaterial(cutoff, BlendMode.SrcAlpha, BlendMode.OneMinusSrcAlpha) };
                }
                case GameBoxBlendFlag.InvertColorBlend:
                    return new[] { CreateBlendedMaterial(DisabledAlphaCutoff, BlendMode.One, BlendMode.OneMinusSrcColor) };
                case GameBoxBlendFlag.AdditiveBlend:
                    return new[] { CreateBlendedMaterial(DisabledAlphaCutoff, BlendMode.One, BlendMode.One) };
                default:
                    throw new ArgumentOutOfRangeException(nameof(blendFlag), blendFlag, "Unknown mesh blend mode");
            }

            IMaterial CreateMeshMaterial(Stack<IMaterial> pool, IMaterial template)
            {
                IMaterial material = RentMaterial(pool, template);
                material.SetMainTexture(mainTexture.texture);
                material.SetColor(TintColorPropertyId, tintColor);
                material.SetFloat(LightmapScalePropertyId, lightmapScale);
                SetShadowTexture(material, shadowTexture.texture);
                return material;
            }

            IMaterial CreateBlendedMaterial(float cutoff, BlendMode sourceBlend, BlendMode destinationBlend)
            {
                IMaterial material = CreateMeshMaterial(_transparentMaterialPool, _transparentMaterial);
                material.SetFloat(TransparentThresholdPropertyId, cutoff);
                material.SetInt(BlendSrcFactorPropertyId, (int)sourceBlend);
                material.SetInt(BlendDstFactorPropertyId, (int)destinationBlend);
                return material;
            }
        }

        public void UpdateMaterial(IMaterial material,
            ITexture2D newMainTexture,
            GameBoxBlendFlag blendFlag)
        {
            if (newMainTexture != null)
            {
                material.SetMainTexture(newMainTexture);
            }

            if (blendFlag == GameBoxBlendFlag.AlphaBlend)
            {
                material.SetFloat(TransparentThresholdPropertyId, DefaultAlphaCutoff);
            }
        }

        /// <inheritdoc/>
        public IMaterial CreateWaterMaterial(
            (string name, ITexture2D texture) mainTexture,
            (string name, ITexture2D texture) shadowTexture,
            float opacity,
            GameBoxBlendFlag blendFlag)
        {
            IMaterial material = RentMaterial(_waterMaterialPool, _waterMaterial);
            material.SetMainTexture(mainTexture.texture);
            material.SetFloat(WaterAlphaPropId, opacity);
            SetShadowTexture(material, shadowTexture.texture);
            return material;
        }

        private IMaterial RentMaterial(Stack<IMaterial> pool, IMaterial template)
        {
            return pool.TryPop(out IMaterial material) ? material : MaterialFactory.CreateMaterialFrom(template);
        }

        private static void SetShadowTexture(IMaterial material, ITexture2D shadowTexture)
        {
            material.SetFloat(HasShadowTexturePropertyId, shadowTexture == null ? 0f : 1f);
            material.SetTexture(ShadowTexturePropertyId, shadowTexture);
        }

        protected override void ReturnToPool(IMaterial material)
        {
            if (material == null || material.IsNativeObjectDisposed) return;

            (Stack<IMaterial> pool, int capacity) = material.ShaderName switch
            {
                WATER_SHADER_PATH => (_waterMaterialPool, WATER_MATERIAL_POOL_SIZE),
                TRANSPARENT_SHADER_PATH => (_transparentMaterialPool, TRANSPARENT_MATERIAL_POOL_SIZE),
                TRANSPARENT_OPAQUE_PART_SHADER_PATH => (_transparentOpaquePartMaterialPool,
                    TRANSPARENT_OPAQUE_PART_MATERIAL_POOL_SIZE),
                OPAQUE_SHADER_PATH => (_opaqueMaterialPool, OPAQUE_MATERIAL_POOL_SIZE),
                _ => (null, 0),
            };

            if (pool == null) return;
            if (_disposed || pool.Count >= capacity)
            {
                material.Destroy();
                return;
            }

            material.SetMainTexture(null);
            material.SetTexture(ShadowTexturePropertyId, null);
            pool.Push(material);
        }
    }
}
