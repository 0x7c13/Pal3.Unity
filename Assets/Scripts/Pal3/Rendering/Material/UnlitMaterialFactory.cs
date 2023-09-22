// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Rendering.Material
{
    using System.Collections.Generic;
    using System.Diagnostics;
    using Core.Primitives;
    using Engine.Extensions;
    using UnityEngine;
    using UnityEngine.Rendering;
    using Color = UnityEngine.Color;
    using Debug = UnityEngine.Debug;

    /// <summary>
    /// Unlit material factory for generating materials that have similar
    /// look and feel as the original game
    /// </summary>
    public class UnlitMaterialFactory : MaterialFactoryBase, IMaterialFactory
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
        private static readonly int WaterAlphaPropId = Shader.PropertyToID("_Alpha");
        private static readonly int WaterHasShadowTexPropId = Shader.PropertyToID("_HasShadowTex");

        // Standard material uniforms for Pal3 unlit shaders
        private static readonly int BlendSrcFactorPropertyId = Shader.PropertyToID("_BlendSrcFactor");
        private static readonly int BlendDstFactorPropertyId = Shader.PropertyToID("_BlendDstFactor");
        private static readonly int TintColorPropertyId = Shader.PropertyToID("_TintColor");
        private static readonly int TransparentThresholdPropertyId = Shader.PropertyToID("_Threshold");
        private static readonly int HasShadowTexturePropertyId = Shader.PropertyToID("_HasShadowTex");
        private static readonly int ShadowTexturePropertyId = Shader.PropertyToID("_ShadowTex");

        private const float DEFAULT_TRANSPARENT_THRESHOLD = 0.9f;

        public MaterialShaderType ShaderType => MaterialShaderType.Unlit;

        private readonly Material _waterMaterial;
        private readonly Material _transparentMaterial;
        private readonly Material _transparentOpaquePartMaterial;
        private readonly Material _opaqueMaterial;

        private const int WATER_MATERIAL_POOL_SIZE = 100;
        private const int TRANSPARENT_MATERIAL_POOL_SIZE = 1500;
        private const int TRANSPARENT_OPAQUE_PART_MATERIAL_POOL_SIZE = 1500;
        private const int OPAQUE_MATERIAL_POOL_SIZE = 2000;

        private readonly Stack<Material> _waterMaterialPool = new (WATER_MATERIAL_POOL_SIZE);
        private readonly Stack<Material> _transparentMaterialPool = new (TRANSPARENT_MATERIAL_POOL_SIZE);
        private readonly Stack<Material> _transparentOpaquePartMaterialPool = new (TRANSPARENT_OPAQUE_PART_MATERIAL_POOL_SIZE);
        private readonly Stack<Material> _opaqueMaterialPool = new (OPAQUE_MATERIAL_POOL_SIZE);

        private bool _isMaterialPoolAllocated = false;

        public UnlitMaterialFactory()
        {
            _waterMaterial = new Material(GetShader(WATER_SHADER_PATH));
            _transparentMaterial = new Material(GetShader(TRANSPARENT_SHADER_PATH));
            _transparentOpaquePartMaterial = new Material(GetShader(TRANSPARENT_OPAQUE_PART_SHADER_PATH));
            _opaqueMaterial = new Material(GetShader(OPAQUE_SHADER_PATH));
        }

        public void AllocateMaterialPool()
        {
            if (_isMaterialPoolAllocated) return;

            var timer = new Stopwatch();
            timer.Start();

            for (var i = 0; i < WATER_MATERIAL_POOL_SIZE; i++)
            {
                _waterMaterialPool.Push(new Material(_waterMaterial)
                {
                    hideFlags = HideFlags.HideAndDontSave
                });
            }

            for (var i = 0; i < TRANSPARENT_MATERIAL_POOL_SIZE; i++)
            {
                _transparentMaterialPool.Push(new Material(_transparentMaterial)
                {
                    hideFlags = HideFlags.HideAndDontSave
                });
            }

            for (var i = 0; i < TRANSPARENT_OPAQUE_PART_MATERIAL_POOL_SIZE; i++)
            {
                _transparentOpaquePartMaterialPool.Push(new Material(_transparentOpaquePartMaterial)
                {
                    hideFlags = HideFlags.HideAndDontSave
                });
            }

            for (var i = 0; i < OPAQUE_MATERIAL_POOL_SIZE; i++)
            {
                _opaqueMaterialPool.Push(new Material(_opaqueMaterial)
                {
                    hideFlags = HideFlags.HideAndDontSave
                });
            }

            timer.Stop();
            Debug.Log($"[{nameof(UnlitMaterialFactory)}] Material pool allocated in {timer.ElapsedMilliseconds} ms.");
        }

        public void DeallocateMaterialPool()
        {
            var timer = new Stopwatch();
            timer.Start();

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

            timer.Stop();
            Debug.Log($"[{nameof(LitMaterialFactory)}] Material pool de-allocated in {timer.ElapsedMilliseconds} ms.");
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

            float transparentThreshold = DEFAULT_TRANSPARENT_THRESHOLD;

            if (shadowTexture.texture == null && rendererType == RendererType.Pol)
            {
                transparentThreshold = 1.0f;
            }

            if (blendFlag is GameBoxBlendFlag.AlphaBlend or GameBoxBlendFlag.InvertColorBlend)
            {
                materials = new Material[2];
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
                materials = new Material[1];
                materials[0] = CreateOpaqueMaterial(mainTexture, shadowTexture, tintColor);
            }

            return materials;
        }

        public void UpdateMaterial(Material material,
            Texture2D newMainTexture,
            GameBoxBlendFlag blendFlag)
        {
            material.mainTexture = newMainTexture;

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
        public Material CreateWaterMaterial(
            (string name, Texture2D texture) mainTexture,
            (string name, Texture2D texture) shadowTexture,
            float opacity,
            GameBoxBlendFlag blendFlag)
        {
            Material material;
            if (_waterMaterialPool.Count > 0)
            {
                material = _waterMaterialPool.Pop();
            }
            else
            {
                material = new Material(_waterMaterial)
                {
                    hideFlags = HideFlags.HideAndDontSave
                };
            }

            material.mainTexture = mainTexture.texture;
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

        private Material CreateTransparentMaterial(
            (string name, Texture2D texture) mainTexture,
            (string name, Texture2D texture) shadowTexture,
            Color tintColor,
            float transparentThreshold)
        {
            Material material;
            if (_transparentMaterialPool.Count > 0)
            {
                material = _transparentMaterialPool.Pop();
            }
            else
            {
                material = new Material(_transparentMaterial)
                {
                    hideFlags = HideFlags.HideAndDontSave
                };
            }

            material.mainTexture = mainTexture.texture;
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

        private Material CreateTransparentOpaquePartMaterial(
            (string name, Texture2D texture) mainTexture,
            (string name, Texture2D texture) shadowTexture,
            Color tintColor,
            float transparentThreshold)
        {
            Material material;
            if (_transparentOpaquePartMaterialPool.Count > 0)
            {
                material = _transparentOpaquePartMaterialPool.Pop();
            }
            else
            {
                material = new Material(_transparentOpaquePartMaterial)
                {
                    hideFlags = HideFlags.HideAndDontSave
                };
            }

            material.mainTexture = mainTexture.texture;
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

        private Material CreateOpaqueMaterial(
            (string name, Texture2D texture) mainTexture,
            (string name, Texture2D texture) shadowTexture,
            Color tintColor)
        {
            Material material;
            if (_opaqueMaterialPool.Count > 0)
            {
                material = _opaqueMaterialPool.Pop();
            }
            else
            {
                material = new Material(_opaqueMaterial)
                {
                    hideFlags = HideFlags.HideAndDontSave
                };
            }

            material.mainTexture = mainTexture.texture;
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

        protected override void ReturnToPool(Material material)
        {
            switch (material.shader.name)
            {
                case WATER_SHADER_NAME:
                    material.mainTexture = null;
                    material.SetTexture(ShadowTexturePropertyId, null);
                    _waterMaterialPool.Push(material);
                    break;
                case TRANSPARENT_SHADER_NAME:
                    material.mainTexture = null;
                    material.SetTexture(ShadowTexturePropertyId, null);
                    _transparentMaterialPool.Push(material);
                    break;
                case TRANSPARENT_OPAQUE_PART_SHADER_NAME:
                    material.mainTexture = null;
                    material.SetTexture(ShadowTexturePropertyId, null);
                    _transparentOpaquePartMaterialPool.Push(material);
                    break;
                case OPAQUE_SHADER_NAME:
                    material.mainTexture = null;
                    material.SetTexture(ShadowTexturePropertyId, null);
                    _opaqueMaterialPool.Push(material);
                    break;
                default:
                    return;
            }
        }
    }
}