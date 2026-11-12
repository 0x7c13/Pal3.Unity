// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

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
    /// Material factory for the original and toon rendering styles.
    /// </summary>
    public sealed class GameMaterialManager : MaterialManagerBase, IMaterialManager
    {
        private const string OpaqueShaderName = "Pal3/Opaque";
        private const string TransparentShaderName = "Pal3/Transparent";
        private const string AlphaCutoutShaderName = "Pal3/TransparentOpaquePart";
        private const string WaterShaderName = "Pal3/Water";
        private const string ToonWaterShaderName = "Pal3/ToonWater";
        private const string ToonOpaqueShaderName = "Pal3/ToonOpaque";
        private const string ToonTransparentShaderName = "Pal3/ToonTransparent";
        private const string ToonCutoutShaderName = "Pal3/ToonCutout";

        // Water material uniforms
        private static readonly int WaterAlphaPropId = ShaderUtility.GetPropertyIdByName("_Alpha");

        // Material properties shared by the original and Toon shaders
        private static readonly int BlendSrcFactorPropertyId = ShaderUtility.GetPropertyIdByName("_BlendSrcFactor");
        private static readonly int BlendDstFactorPropertyId = ShaderUtility.GetPropertyIdByName("_BlendDstFactor");
        private static readonly int TintColorPropertyId = ShaderUtility.GetPropertyIdByName("_TintColor");
        private static readonly int TransparentThresholdPropertyId = ShaderUtility.GetPropertyIdByName("_Threshold");
        private static readonly int HasBakedLightmapId = ShaderUtility.GetPropertyIdByName("_HasShadowTex");
        private static readonly int LightmapScalePropertyId = ShaderUtility.GetPropertyIdByName("_LightmapScale");
        private static readonly int ReceiveShadowsPropertyId = ShaderUtility.GetPropertyIdByName("_ReceiveShadows");
        private static readonly int CharacterPropertyId = ShaderUtility.GetPropertyIdByName("_IsCharacter");
        private static readonly int ReceiveLightingPropertyId = ShaderUtility.GetPropertyIdByName("_ReceiveLighting");
        private static readonly int BakedLightmapId = ShaderUtility.GetPropertyIdByName("_ShadowTex");

        private const float DefaultAlphaCutoff = 0.9f;
        private const float PolAlphaCutoff = 245f / 255f;
        private const float DisabledAlphaCutoff = 2f;
        private const float DefaultLightmapScale = 1f / 0.6f;
        private const float PolLightmapScale = 2f;

        public bool UsesToonRendering { get; }

        private readonly IMaterial _waterMaterial;
        private readonly IMaterial _transparentMaterial;
        private readonly IMaterial _alphaCutoutMaterial;
        private readonly IMaterial _opaqueMaterial;

        private const int WATER_MATERIAL_POOL_SIZE = 100;
        private const int TRANSPARENT_MATERIAL_POOL_SIZE = 1500;
        private const int ALPHA_CUTOUT_MATERIAL_POOL_SIZE = 1500;
        private const int OPAQUE_MATERIAL_POOL_SIZE = 2000;

        private readonly Stack<IMaterial> _waterMaterialPool = new();
        private readonly Stack<IMaterial> _transparentMaterialPool = new();
        private readonly Stack<IMaterial> _alphaCutoutMaterialPool = new();
        private readonly Stack<IMaterial> _opaqueMaterialPool = new();

        private bool _disposed;

        public GameMaterialManager(IMaterialFactory materialFactory, bool usesToonRendering) : base(materialFactory)
        {
            UsesToonRendering = usesToonRendering;
            _waterMaterial = MaterialFactory.CreateMaterial(usesToonRendering ? ToonWaterShaderName : WaterShaderName);
            _transparentMaterial = MaterialFactory.CreateMaterial(usesToonRendering ? ToonTransparentShaderName : TransparentShaderName);
            _alphaCutoutMaterial = MaterialFactory.CreateMaterial(usesToonRendering ? ToonCutoutShaderName : AlphaCutoutShaderName);
            _opaqueMaterial = MaterialFactory.CreateMaterial(usesToonRendering ? ToonOpaqueShaderName : OpaqueShaderName);
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            DisposePool(_waterMaterialPool);
            DisposePool(_transparentMaterialPool);
            DisposePool(_alphaCutoutMaterialPool);
            DisposePool(_opaqueMaterialPool);
            _waterMaterial.Destroy();
            _transparentMaterial.Destroy();
            _alphaCutoutMaterial.Destroy();
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
            (string name, ITexture2D texture) bakedLightmap,
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
                    IMaterial depthWriting = CreateMeshMaterial(_alphaCutoutMaterialPool,
                        _alphaCutoutMaterial);
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
                SetBakedLightmap(material, bakedLightmap.texture);
                if (UsesToonRendering)
                {
                    material.SetFloat(ReceiveShadowsPropertyId, 1f);
                    material.SetFloat(CharacterPropertyId, rendererType is RendererType.Mv3 or RendererType.Msh ? 1f : 0f);
                }
                return material;
            }

            IMaterial CreateBlendedMaterial(float cutoff, BlendMode sourceBlend, BlendMode destinationBlend)
            {
                IMaterial material = CreateMeshMaterial(_transparentMaterialPool, _transparentMaterial);
                material.SetFloat(TransparentThresholdPropertyId, cutoff);
                material.SetInt(BlendSrcFactorPropertyId, (int)sourceBlend);
                material.SetInt(BlendDstFactorPropertyId, (int)destinationBlend);
                if (UsesToonRendering)
                {
                    // Additive and inverse-color meshes are emissive in the original assets.
                    material.SetFloat(ReceiveLightingPropertyId, sourceBlend == BlendMode.SrcAlpha ? 1f : 0f);
                }
                return material;
            }
        }

        /// <inheritdoc/>
        public IMaterial CreateWaterMaterial(
            (string name, ITexture2D texture) mainTexture,
            (string name, ITexture2D texture) bakedLightmap,
            float opacity)
        {
            IMaterial material = RentMaterial(_waterMaterialPool, _waterMaterial);
            material.SetMainTexture(mainTexture.texture);
            material.SetFloat(WaterAlphaPropId, opacity);
            if (UsesToonRendering) material.SetFloat(ReceiveShadowsPropertyId, 1f);
            SetBakedLightmap(material, bakedLightmap.texture);
            return material;
        }

        private IMaterial RentMaterial(Stack<IMaterial> pool, IMaterial template)
        {
            return pool.TryPop(out IMaterial material) ? material : MaterialFactory.CreateMaterialFrom(template);
        }

        private static void SetBakedLightmap(IMaterial material, ITexture2D bakedLightmap)
        {
            material.SetFloat(HasBakedLightmapId, bakedLightmap == null ? 0f : 1f);
            material.SetTexture(BakedLightmapId, bakedLightmap);
        }

        protected override void ReturnToPool(IMaterial material)
        {
            if (material == null || material.IsNativeObjectDisposed) return;

            (Stack<IMaterial> pool, int capacity) = material.ShaderName switch
            {
                WaterShaderName or ToonWaterShaderName => (_waterMaterialPool, WATER_MATERIAL_POOL_SIZE),
                TransparentShaderName or ToonTransparentShaderName => (_transparentMaterialPool, TRANSPARENT_MATERIAL_POOL_SIZE),
                AlphaCutoutShaderName or ToonCutoutShaderName => (_alphaCutoutMaterialPool,
                    ALPHA_CUTOUT_MATERIAL_POOL_SIZE),
                OpaqueShaderName or ToonOpaqueShaderName => (_opaqueMaterialPool, OPAQUE_MATERIAL_POOL_SIZE),
                _ => (null, 0),
            };

            if (pool == null) return;
            if (_disposed || pool.Count >= capacity)
            {
                material.Destroy();
                return;
            }

            material.SetMainTexture(null);
            material.SetTexture(BakedLightmapId, null);
            pool.Push(material);
        }
    }
}
