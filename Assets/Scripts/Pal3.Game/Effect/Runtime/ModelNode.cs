// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Game.Effect.Runtime
{
    using System;
    using System.Collections.Generic;
    using UnityEngine.Rendering;
    using Core.DataReader.Data;
    using Engine.Core.Implementation;
    using Rendering.Material;
    using Rendering.Renderer;
    using Rendering.Native;
    using UnityEngine;

    internal sealed class ModelNode : EffectNode
    {
        private const float ReferenceFramesPerSecond = 60f;
        private readonly CvdModelRenderer _model;
        private readonly PolyModelRenderer _poly;
        private readonly GameMaterialManager _materials;
        private readonly Renderer[] _renderers;
        private float _animationStart;
        private float _opacity = 1;
        private readonly List<Material> _opacityMaterials = new();
        private readonly MaterialPropertyBlock _properties = new();

        public ModelNode(GameObject root, EffectDefinition d, EffectTask[] tasks, Vector3 origin, float birth, EffectResources resources,
            GameBoxNativeLighting nativeLighting = null)
            : base(root, d, tasks, origin, birth)
        {
            // CEffModel ignores the flexeff tint/additive/depth flags: the model
            // owns those settings. SetBlend(ALPHA) overrides them only for fades.
            _materials = new GameMaterialManager(new UnityMaterialFactory(), false, nativeLighting);
            try
            {
                if (d.TextureName.EndsWith(".cvd", StringComparison.OrdinalIgnoreCase))
                {
                    _model = root.AddComponent<CvdModelRenderer>();
                    _model.Init(resources.Model(d.TextureName), resources.ModelTextureProvider(d.TextureName), _materials);
                }
                else if (d.TextureName.EndsWith(".pol", StringComparison.OrdinalIgnoreCase))
                {
                    _poly = root.AddComponent<PolyModelRenderer>();
                    _poly.Render(resources.PolModel(d.TextureName), resources.ModelTextureProvider(d.TextureName), _materials,
                        isStaticObject: false, textureFactory: new UnityTextureFactory(), animateWater: false,
                        modelMode: PolModelMode.Authored);
                }
                else
                    throw new NotSupportedException($"Unsupported original model {d.TextureName}");
            }
            catch
            {
                _model?.Dispose();
                _poly?.Dispose();
                _materials.Dispose();
                throw;
            }
            _renderers = root.GetComponentsInChildren<Renderer>();
            if (_poly != null)
                foreach (Renderer renderer in _renderers)
                {
                    renderer.shadowCastingMode = ShadowCastingMode.Off;
                    renderer.receiveShadows = false;
                }
            foreach (Transform child in root.GetComponentsInChildren<Transform>())
                child.gameObject.layer = root.layer;
        }

        private void EnableOpacityOverride()
        {
            if (_opacityMaterials.Count > 0)
                return;
            Shader shader = Shader.Find("Pal3/Transparent");
            if (shader == null)
                throw new System.InvalidOperationException("The effect opacity shader is missing.");
            foreach (Renderer renderer in _renderers)
            {
                if (renderer.sharedMaterial == null)
                    continue;
                var material = new Material(renderer.sharedMaterial)
                {
                    shader = shader,
                    hideFlags = HideFlags.DontSave
                };
                material.SetInt("_BlendSrcFactor", (int)BlendMode.SrcAlpha);
                material.SetInt("_BlendDstFactor", (int)BlendMode.OneMinusSrcAlpha);
                material.SetFloat("_Threshold", 2);
                material.renderQueue = (int)RenderQueue.Transparent;
                _opacityMaterials.Add(material);
                // Existing CVD/POL cutout + blend passes become a single alpha pass.
                renderer.sharedMaterials = new[] { material };
            }
        }

        protected override void Apply(EffectTask t, EffectTaskClock clock, float dt)
        {
            if (clock.StartedNow)
                _animationStart = LocalTime;
            base.Apply(t, clock, dt);
            if (t.SpeedAlpha != 0)
            {
                EnableOpacityOverride();
                _opacity = Mathf.Clamp01(_opacity + t.SpeedAlpha * dt);
            }
        }

        protected override void ApplyRotationAndStretch(EffectTask t, float dt)
        {
            // CEffModel.cpp:238-245 adds these values once per update. Normalize
            // to our chosen 60 FPS compatibility reference, independently of
            // the display frame rate and the editor/seek integration step.
            base.ApplyRotationAndStretch(t, dt * ReferenceFramesPerSecond);
        }

        public override void Render(Camera camera)
        {
            Root.transform.localPosition = UnityPosition(Position + Orbit);
            Root.transform.localRotation = UnityRotation(Rotation);
            Root.transform.localScale = Scale;
            foreach (Renderer renderer in _renderers)
            {
                renderer.enabled = Alive && Visible;
                if (_opacityMaterials.Count > 0)
                {
                    renderer.GetPropertyBlock(_properties);
                    _properties.SetColor("_TintColor", new Color(1, 1, 1, _opacity));
                    renderer.SetPropertyBlock(_properties);
                }
            }
            if (Alive && Visible)
                _model?.SetCurrentTime(Mathf.Max(0, LocalTime - _animationStart));
        }

        public override void Dispose()
        {
            _model?.Dispose();
            _poly?.Dispose();
            foreach (Material material in _opacityMaterials)
                EffectResources.Destroy(material);
            _materials.Dispose();
            base.Dispose();
        }
    }
}
