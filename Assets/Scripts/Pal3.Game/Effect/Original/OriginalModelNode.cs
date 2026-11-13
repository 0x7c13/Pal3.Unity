// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Game.Effect.Original
{
    using System;
    using Core.DataReader.Data;
    using Engine.Core.Implementation;
    using Rendering.Material;
    using Rendering.Renderer;
    using UnityEngine;

    internal sealed class OriginalModelNode : OriginalEffectNode
    {
        private const float ReferenceFramesPerSecond = 60f;
        private readonly CvdModelRenderer _model;
        private readonly GameMaterialManager _materials;
        private readonly Renderer[] _renderers;
        private float _animationStart;

        public OriginalModelNode(GameObject root, EffectDefinition d, EffectTask[] tasks, Vector3 origin, float birth, OriginalEffectResources resources)
            : base(root, d, tasks, origin, birth)
        {
            ValidateTasks(Tasks);
            if (!d.TextureName.EndsWith(".cvd", StringComparison.OrdinalIgnoreCase))
                throw new NotSupportedException($"Original model adapter requires CVD: {d.TextureName}");
            // Use the same texture resolution and existing CVD renderer as the game.
            _materials = new GameMaterialManager(new UnityMaterialFactory(), false);
            _model = root.AddComponent<CvdModelRenderer>();
            try
            {
                _model.Init(resources.Model(d.TextureName), resources.ModelTextureProvider(d.TextureName), _materials);
            }
            catch
            {
                _model.Dispose();
                _materials.Dispose();
                throw;
            }
            _renderers = root.GetComponentsInChildren<Renderer>();
            foreach (Transform child in root.GetComponentsInChildren<Transform>())
                child.gameObject.layer = root.layer;
        }

        internal static void ValidateTasks(EffectTask[] tasks)
        {
            // CEffModel uses normalized alpha, unlike the 0..255 color tasks.
            // The CVD renderer does not yet expose its model-wide blend override.
            if (Array.Exists(tasks, task => task.SpeedAlpha != 0))
                throw new NotSupportedException("Original CVD model opacity tasks are not implemented");
        }

        protected override void Apply(EffectTask t, OriginalTaskClock clock, float dt)
        {
            if (clock.StartedNow)
                _animationStart = LocalTime;
            base.Apply(t, clock, dt);
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
                renderer.enabled = Alive && Visible;
            if (Alive && Visible)
                _model.SetCurrentTime(Mathf.Max(0, LocalTime - _animationStart));
        }

        public override void Dispose()
        {
            _model.Dispose();
            _materials.Dispose();
            base.Dispose();
        }
    }
}
