// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Game.Rendering.Native
{
    using System;
    using System.Collections.Generic;
    using Core.DataReader.Lgt;
    using Core.Primitives;
    using Core.Utilities;
    using Engine.Extensions;
    using UnityEngine;

    /// <summary>Original per-object, two-light selection. Each preview owns its context.</summary>
    public sealed class GameBoxNativeLighting : IDisposable
    {
        private readonly bool _pal3A;
        private LightNode[] _lights = Array.Empty<LightNode>();
        private readonly List<Binding> _bindings = new();
        private readonly MaterialPropertyBlock _properties = new();
        private bool _disposed;
        private bool _hasLightFile;
        private RuntimeLight[] _candidates = new RuntimeLight[1];
        private float[] _weights = new float[1];
        private int[] _order = new int[1];

        private static readonly int CountId = Shader.PropertyToID("_GbLightCount");
        private static readonly int UnitsId = Shader.PropertyToID("_GbVertexUnits");
        private static readonly int VertexColorId = Shader.PropertyToID("_GbVertexColor");
        private static readonly int SourceFromRendererId = Shader.PropertyToID("_GbSourceFromRenderer");
        private static readonly int[] PositionIds = { Shader.PropertyToID("_GbLightPosition0"), Shader.PropertyToID("_GbLightPosition1") };
        private static readonly int[] DiffuseIds = { Shader.PropertyToID("_GbLightDiffuse0"), Shader.PropertyToID("_GbLightDiffuse1") };
        private static readonly int[] AmbientIds = { Shader.PropertyToID("_GbLightAmbient0"), Shader.PropertyToID("_GbLightAmbient1") };
        private static readonly int[] AttenuationIds = { Shader.PropertyToID("_GbLightAttenuation0"), Shader.PropertyToID("_GbLightAttenuation1") };

        public GameBoxNativeLighting(string variant)
        {
            _pal3A = string.Equals(variant, "PAL3A", StringComparison.OrdinalIgnoreCase);
            if (!_pal3A && !string.Equals(variant, "PAL3", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Unknown GameBox lighting variant.", nameof(variant));
        }

        public void SetLights(LgtFile lights)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(GameBoxNativeLighting));
            _hasLightFile = lights != null;
            _lights = lights == null ? Array.Empty<LightNode>() : (LightNode[])lights.LightNodes.Clone();
            _candidates = new RuntimeLight[_lights.Length + 1];
            _weights = new float[_candidates.Length];
            _order = new int[_candidates.Length];
        }

        public IDisposable Bind(MeshRenderer renderer, Vector3 anchor, bool lightmapped,
            bool vertexColors, bool environment, float vertexUnits, Func<Matrix4x4> sourceToRenderer = null)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(GameBoxNativeLighting));
            if (renderer == null)
                throw new ArgumentNullException(nameof(renderer));
            var binding = new Binding(this, renderer, anchor, lightmapped, vertexColors, environment, vertexUnits, sourceToRenderer);
            _bindings.Add(binding);
            return binding;
        }

        public void Apply(Camera camera)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(GameBoxNativeLighting));
            if (camera == null)
                throw new ArgumentNullException(nameof(camera));
            foreach (Binding binding in _bindings)
            {
                MeshRenderer renderer = binding.Renderer;
                if (renderer == null)
                    continue;
                renderer.GetPropertyBlock(_properties);
                _properties.SetFloat(UnitsId, binding.VertexUnits);
                _properties.SetFloat(VertexColorId, binding.VertexColors ? 1 : 0);
                Matrix4x4 sourceToRenderer = binding.SourceToRenderer?.Invoke() ?? Matrix4x4.identity;
                Matrix4x4 sourceFromRenderer = binding.SourceToRenderer == null ? Matrix4x4.identity : sourceToRenderer.inverse;
                _properties.SetMatrix(SourceFromRendererId, sourceFromRenderer);
                int count = 0;
                if (!binding.Lightmapped && !binding.VertexColors)
                {
                    Vector3 anchor = renderer.transform.TransformPoint(sourceToRenderer.MultiplyPoint3x4(binding.Anchor));
                    // PAL3 attaches its default before POL, then attaches LGT before actors.
                    // PAL3A attaches LGT before both, using the default only when LGT is absent.
                    if (!_pal3A || !_hasLightFile)
                        Consider(DefaultLight());
                    if (_pal3A || !binding.Environment)
                        foreach (LightNode light in _lights)
                            if (light.LightDecayType == GameBoxLightDecayType.None || ValidDecayRadius(light.DecayRadius))
                                Consider(Convert(light, camera.transform.position));

                    void Consider(RuntimeLight light)
                    {
                        float weight = Influence(light, anchor);
                        if (weight == 0 || !float.IsFinite(weight))
                            return;
                        _candidates[count] = light;
                        _weights[count] = weight;
                        count++;
                    }
                }
                GameBoxLightOrder.Sort(_order.AsSpan(0, count), _weights.AsSpan(0, count));
                int selected = Math.Min(2, count);
                for (int index = 0; index < selected; index++)
                    Upload(_candidates[_order[index]], index);
                _properties.SetFloat(CountId, selected);
                renderer.SetPropertyBlock(_properties);

                void Upload(RuntimeLight light, int index)
                {
                    // Source shaders transform lights into object coordinates. Preserve
                    // that space instead of normalizing inverse-transpose world normals.
                    Vector3 local = sourceFromRenderer.MultiplyPoint3x4(renderer.transform.InverseTransformPoint(light.Position)) * UnityPrimitivesConvertor.GameBoxUnitToUnityUnit;
                    _properties.SetVector(PositionIds[index], local);
                    _properties.SetVector(DiffuseIds[index], light.Diffuse);
                    _properties.SetVector(AmbientIds[index], light.Ambient);
                    _properties.SetVector(AttenuationIds[index], light.Attenuation);
                }
            }
        }

        private static RuntimeLight Convert(LightNode light, Vector3 camera)
        {
            GameBoxMatrix4x4 matrix = light.GameBoxWorldMatrix;
            Vector3 position = new(-matrix.Tx, matrix.Ty, matrix.Tz);
            if (light.LightType == GameBoxLightType.Directional)
                position = camera * UnityPrimitivesConvertor.GameBoxUnitToUnityUnit -
                    new Vector3(matrix.Yx, -matrix.Yy, -matrix.Yz) * 100000;
            return new RuntimeLight
            {
                Position = position / UnityPrimitivesConvertor.GameBoxUnitToUnityUnit,
                Diffuse = light.UseDiffuse ? new Vector4(light.LightColor.R, light.LightColor.G, light.LightColor.B, 0) : Vector4.zero,
                Ambient = new Vector4(light.AmbientColor.R, light.AmbientColor.G, light.AmbientColor.B, 0),
                Attenuation = Attenuation(light.LightDecayType, light.DecayRadius),
            };
        }

        private static RuntimeLight DefaultLight() => new()
        {
            Diffuse = new Vector4(.01f, .01f, .01f, 0),
            Ambient = new Vector4(-.25f, -.25f, -.25f, 0),
            Attenuation = Attenuation(GameBoxLightDecayType.Distance, 300),
        };

        private static bool ValidDecayRadius(float radius) => float.IsFinite(radius) && radius >= 1e-10f;

        public static Vector4 Attenuation(GameBoxLightDecayType type, float radius) => type switch
        {
            GameBoxLightDecayType.None => new Vector4(1, 0, 0, 0),
            GameBoxLightDecayType.Distance when ValidDecayRadius(radius) => new Vector4(.01f, 1 / radius, 0, 0),
            GameBoxLightDecayType.Square when ValidDecayRadius(radius) => new Vector4(.01f, 0, 1 / (radius * radius), 0),
            GameBoxLightDecayType.Distance or GameBoxLightDecayType.Square => Vector4.zero,
            _ => throw new ArgumentOutOfRangeException(nameof(type)),
        };

        private static float Influence(RuntimeLight light, Vector3 point)
        {
            float distance = Vector3.Distance(light.Position, point) * UnityPrimitivesConvertor.GameBoxUnitToUnityUnit;
            Vector4 attenuation = light.Attenuation;
            float effect = Mathf.Min(1, 1 / (attenuation.x + attenuation.y * distance + attenuation.z * distance * distance));
            return effect * (light.Diffuse.x + light.Diffuse.y + light.Diffuse.z) / 3;
        }

        public void Dispose()
        {
            _disposed = true;
            _bindings.Clear();
            _lights = Array.Empty<LightNode>();
        }

        private struct RuntimeLight
        {
            public Vector3 Position;
            public Vector4 Diffuse;
            public Vector4 Ambient;
            public Vector4 Attenuation;
        }

        private sealed class Binding : IDisposable
        {
            private readonly GameBoxNativeLighting _owner;
            internal readonly MeshRenderer Renderer;
            internal readonly Vector3 Anchor;
            internal readonly bool Lightmapped;
            internal readonly bool VertexColors;
            internal readonly bool Environment;
            internal readonly float VertexUnits;
            internal readonly Func<Matrix4x4> SourceToRenderer;

            internal Binding(GameBoxNativeLighting owner, MeshRenderer renderer, Vector3 anchor,
                bool lightmapped, bool vertexColors, bool environment, float vertexUnits, Func<Matrix4x4> sourceToRenderer)
            {
                _owner = owner;
                Renderer = renderer;
                Anchor = anchor;
                Lightmapped = lightmapped;
                VertexColors = vertexColors;
                Environment = environment;
                VertexUnits = vertexUnits;
                SourceToRenderer = sourceToRenderer;
            }

            public void Dispose() => _owner._bindings.Remove(this);
        }
    }
}
