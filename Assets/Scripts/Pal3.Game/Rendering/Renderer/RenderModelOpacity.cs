// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Game.Rendering.Renderer
{
    using System;
    using System.Collections.Generic;
    using Core.Primitives;
    using Engine.Core.Abstraction;
    using Engine.Renderer;
    using Engine.Services;
    using Material;
    using Color = Core.Primitives.Color;

    // Each model owns these rented materials. A fade changes uniforms; only the
    // first opaque-to-transparent transition rents an additional material pair.
    internal sealed class RenderModelOpacity : IDisposable
    {
        private static readonly int TintId = ShaderUtility.GetPropertyIdByName("_TintColor");
        private static readonly int ThresholdId = ShaderUtility.GetPropertyIdByName("_Threshold");
        private static readonly int WaterAlphaId = ShaderUtility.GetPropertyIdByName("_Alpha");
        private static readonly int VertexColorId = ShaderUtility.GetPropertyIdByName("_GbVertexColor");

        private readonly IMaterialManager _manager;
        private readonly GameBoxMaterial? _sourceMaterial;
        private readonly List<IDisposable> _lightingBindings = new();
        private readonly RendererType _rendererType;
        private readonly (string name, ITexture2D texture) _lightmap;
        private readonly Color _tint;
        private readonly GameBoxBlendFlag _blend;
        private readonly float? _waterAlpha;
        private readonly Action<IMaterial[], string> _configure;
        private readonly Action<IMaterial[]> _returnMaterials;
        private readonly List<StaticMeshRenderer> _renderers = new();
        private readonly float[] _thresholds;
        private (string name, ITexture2D texture) _texture;
        private IMaterial[] _normal;
        private IMaterial[] _faded;

        public IMaterial[] Materials
        {
            get; private set;
        }

        public RenderModelOpacity(IMaterialManager manager, RendererType rendererType,
            (string name, ITexture2D texture) texture, (string name, ITexture2D texture) lightmap,
            Color tint, GameBoxBlendFlag blend, float? waterAlpha = null,
            Action<IMaterial[], string> configure = null, Action<IMaterial[]> returnMaterials = null,
            GameBoxMaterial? sourceMaterial = null)
        {
            _manager = manager;
            _sourceMaterial = sourceMaterial;
            _rendererType = rendererType;
            _texture = texture;
            _lightmap = lightmap;
            _tint = tint;
            _blend = blend;
            _waterAlpha = waterAlpha;
            _configure = configure;
            _returnMaterials = returnMaterials ?? manager.ReturnToPool;
            _normal = waterAlpha.HasValue
                ? new[] { manager.CreateWaterMaterial(texture, lightmap, waterAlpha.Value) }
                : manager.CreateStandardMaterials(rendererType, texture, lightmap, tint, blend, sourceMaterial);
            Materials = _normal;
            _configure?.Invoke(_normal, texture.name);
            if (!waterAlpha.HasValue && blend != GameBoxBlendFlag.Opaque)
            {
                _thresholds = new float[_normal.Length];
                for (int i = 0; i < _normal.Length; i++)
                    _thresholds[i] = _normal[i].GetFloat(ThresholdId);
            }
        }

        public static float Normalize(float value)
        {
            if (!float.IsFinite(value))
                throw new ArgumentOutOfRangeException(nameof(value), "Model opacity must be finite.");
            return Math.Clamp(value, 0f, 1f);
        }

        public void Bind(StaticMeshRenderer renderer, bool apply = true, UnityEngine.Vector3 anchor = default,
            bool vertexColors = false, bool environment = false, float vertexUnits = 20f)
        {
            _renderers.Add(renderer);
            if (_manager.NativeLighting != null)
                _lightingBindings.Add(_manager.NativeLighting.Bind(renderer.GetComponent<UnityEngine.MeshRenderer>(),
                    anchor, _lightmap.texture != null, vertexColors, environment, vertexUnits));
            else if (vertexColors)
            {
                var meshRenderer = renderer.GetComponent<UnityEngine.MeshRenderer>();
                var properties = new UnityEngine.MaterialPropertyBlock();
                meshRenderer.GetPropertyBlock(properties);
                properties.SetFloat(VertexColorId, 1);
                meshRenderer.SetPropertyBlock(properties);
            }
            if (apply)
                renderer.SetMaterials(Materials);
        }

        public void SetOpacity(float opacity)
        {
            IMaterial[] previous = Materials;
            if (!_waterAlpha.HasValue && _blend == GameBoxBlendFlag.Opaque && opacity < 1f)
            {
                if (_faded == null)
                {
                    _faded = _manager.CreateStandardMaterials(_rendererType, _texture, _lightmap, _tint, GameBoxBlendFlag.AlphaBlend, _sourceMaterial);
                    _configure?.Invoke(_faded, _texture.name);
                }
                Materials = _faded;
            }
            else
                Materials = _normal;

            if (_waterAlpha.HasValue)
                Materials[0].SetFloat(WaterAlphaId, _waterAlpha.Value * opacity);
            else
            {
                Color tint = _tint;
                tint.A *= opacity;
                if (_blend is GameBoxBlendFlag.AdditiveBlend or GameBoxBlendFlag.InvertColorBlend)
                {
                    tint.R *= opacity;
                    tint.G *= opacity;
                    tint.B *= opacity;
                }
                for (int i = 0; i < Materials.Length; i++)
                {
                    Materials[i].SetColor(TintId, tint);
                    if (Materials == _faded || _blend == GameBoxBlendFlag.AlphaBlend)
                    {
                        // Both shaders classify raw texture alpha before tinting.
                        // Disable the opaque part and let the blended part draw all
                        // pixels during a fade, including originally opaque texels.
                        Materials[i].SetFloat(ThresholdId, opacity < 1f ? 2f : _thresholds[i]);
                    }
                    else if (_thresholds != null)
                        Materials[i].SetFloat(ThresholdId, opacity == 0f ? 0f : _thresholds[i]);
                }
            }

            if (previous != Materials)
                foreach (StaticMeshRenderer renderer in _renderers)
                    renderer.SetMaterials(Materials);
        }

        public void ChangeTexture(string name, ITexture2D texture)
        {
            _texture = (name, texture);
            UpdateTexture(_normal);
            if (_faded != null)
                UpdateTexture(_faded);
        }

        private void UpdateTexture(IMaterial[] materials)
        {
            foreach (IMaterial material in materials)
                material.SetMainTexture(_texture.texture);
            _configure?.Invoke(materials, _texture.name);
        }

        public void Dispose()
        {
            foreach (IDisposable binding in _lightingBindings)
                binding.Dispose();
            _lightingBindings.Clear();
            _renderers.Clear();
            if (_normal != null)
                _returnMaterials(_normal);
            if (_faded != null)
                _returnMaterials(_faded);
            _normal = _faded = Materials = null;
        }
    }
}
