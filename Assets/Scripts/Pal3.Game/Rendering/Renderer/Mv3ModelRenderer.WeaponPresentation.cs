// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Game.Rendering.Renderer
{
    using System;
    using System.Collections.Generic;
    using Core.DataReader.Pol;
    using Engine.Extensions;
    using UnityEngine;
    using UnityEngine.Rendering;

    public sealed partial class Mv3ModelRenderer
    {
        private readonly Dictionary<string, WeaponBlade> _weaponBlades = new(StringComparer.OrdinalIgnoreCase);
        private Material _weaponBladeMaterial;
        private bool _leftWeaponVisible = true, _rightWeaponVisible = true;
        private double? _lastWeaponClock;
        private bool _preparedSinceFlush, _preparedBodyAdvanced;
        private float _bladeRate = 1, _bladeSeconds = .12f;
        private bool _hasNativeFlush;

        public bool UseRenderBladeClock
        {
            get; set;
        }

        public bool RightWeaponVisible => _rightWeaponVisible;

        public void ResetWeaponPresentation() => DisposeWeaponPresentation();

        public bool TryGetRightWeaponBladeTip(out Vector3 position)
        {
            position = default;
            return _weaponBlades.TryGetValue("tag_weapon1", out WeaponBlade blade) && blade.TryGetLastPosition(out position);
        }

        public void SetWeaponVisibility(bool left, bool right)
        {
            _leftWeaponVisible = left;
            _rightWeaponVisible = right;
            if (_tagNodesInfo == null || _tagNodes == null)
                return;
            for (int index = 0; index < _tagNodesInfo.Length; index++)
            {
                if (_tagNodes[index]?.NativeObject is not GameObject weapon)
                    continue;
                string tag = _tagNodesInfo[index].Name;
                if (tag.Equals("tag_weapon1", StringComparison.OrdinalIgnoreCase))
                    SetWeaponRendering(weapon, right);
                else if (tag.Equals("tag_weapon2", StringComparison.OrdinalIgnoreCase))
                    SetWeaponRendering(weapon, left);
            }
        }

        private static void SetWeaponRendering(GameObject weapon, bool visible)
        {
            // Disabling the object invokes PolyModelRenderer.Dispose. Weapon
            // attachment flags only control drawing and must retain geometry.
            foreach (UnityEngine.Renderer renderer in weapon.GetComponentsInChildren<UnityEngine.Renderer>(true))
                renderer.enabled = visible;
        }

        // Stage the current body/weapon state after sampling transforms. PAL3
        // consumes it once per displayed frame; compatibility mode samples here.
        public void SampleWeaponBlades(double actorClock, float playbackRate, float trailSeconds)
        {
            if ((_tagNodePolFile?.TagNodes == null || _tagNodePolFile.TagNodes.Length < 2 || _tagNodes == null) && _weaponBlades.Count == 0)
                return;
            if (!double.IsFinite(actorClock) || !float.IsFinite(playbackRate) || playbackRate < 0 || !float.IsFinite(trailSeconds) || trailSeconds <= 0)
                throw new ArgumentException("Invalid original weapon blade clock.");
            if (_lastWeaponClock.HasValue && actorClock < _lastWeaponClock.Value)
                DisposeWeaponPresentation();
            float delta = (float)(actorClock - (_lastWeaponClock ?? actorClock));
            // Idempotent synchronization must not revoke an advance that this
            // displayed frame has prepared but has not flushed yet.
            _preparedBodyAdvanced = _preparedSinceFlush && _preparedBodyAdvanced ||
                !_lastWeaponClock.HasValue || actorClock > _lastWeaponClock.Value;
            _preparedSinceFlush = true;
            _lastWeaponClock = actorClock;
            _bladeRate = playbackRate;
            _bladeSeconds = trailSeconds;
            if (!UseRenderBladeClock)
                SampleBladePoses(delta, false, true);
        }

        private void SampleBladePoses(float delta, bool poseOnly, bool bodyVisible)
        {
            var sampled = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int index = 0; index < (_tagNodesInfo?.Length ?? 0); index++)
            {
                if (_tagNodes?[index]?.NativeObject is not GameObject weapon)
                    continue;
                string tag = _tagNodesInfo[index].Name;
                bool right = tag.Equals("tag_weapon1", StringComparison.OrdinalIgnoreCase);
                bool left = tag.Equals("tag_weapon2", StringComparison.OrdinalIgnoreCase);
                if (!left && !right)
                    continue;
                if (!_weaponBlades.TryGetValue(tag, out WeaponBlade blade))
                    _weaponBlades.Add(tag, blade = new WeaponBlade(_tagNodePolFile.TagNodes));
                bool visible = bodyVisible && (right ? _rightWeaponVisible : _leftWeaponVisible);
                if (poseOnly)
                    blade.SamplePose(weapon.transform, visible, _bladeSeconds, rebuildMesh: false);
                else
                    blade.Sample(weapon.transform, visible, delta, _bladeRate, _bladeSeconds);
                sampled.Add(tag);
            }
            foreach (var pair in _weaponBlades)
                if (!sampled.Contains(pair.Key))
                {
                    if (poseOnly)
                        pair.Value.SamplePose(null, false, _bladeSeconds, rebuildMesh: false);
                    else
                        pair.Value.Sample(null, false, delta, _bladeRate, _bladeSeconds);
                }
        }

        // Transfer only when the caller has verified the same equipped POL.
        // Keys are semantic hand tags, so reordering or absent tags in a new
        // actor clip cannot remap a right-hand trail onto the left hand.
        public void MoveWeaponPresentationFrom(Mv3ModelRenderer previous)
        {
            if (previous == null || ReferenceEquals(previous, this))
                return;
            DisposeWeaponPresentation();
            foreach (var pair in previous._weaponBlades)
                _weaponBlades.Add(pair.Key, pair.Value);
            previous._weaponBlades.Clear();
            _weaponBladeMaterial = previous._weaponBladeMaterial;
            previous._weaponBladeMaterial = null;
            _lastWeaponClock = previous._lastWeaponClock;
            previous._lastWeaponClock = null;
            _preparedSinceFlush = previous._preparedSinceFlush;
            _preparedBodyAdvanced = previous._preparedBodyAdvanced;
            _bladeRate = previous._bladeRate;
            _bladeSeconds = previous._bladeSeconds;
            _hasNativeFlush = previous._hasNativeFlush;
            _leftWeaponVisible = previous._leftWeaponVisible;
            _rightWeaponVisible = previous._rightWeaponVisible;
        }

        public void RenderWeaponBlades(Camera camera)
        {
            if (_weaponBlades.Count == 0 || camera == null)
                return;
            if (_weaponBladeMaterial == null)
            {
                _weaponBladeMaterial = WeaponBlade.CreateMaterial();
            }
            foreach (WeaponBlade blade in _weaponBlades.Values)
                blade.Render(camera, _weaponBladeMaterial, GameEntity.Layer);
        }

        // PAL3's two Scene.End calls each Update then Flush all retained records.
        // The owner supplies one stable presentation-frame delta for both calls.
        public void RenderPublishedWeaponBlades(Camera camera, float frameDelta, bool newFrame, bool bodyVisible)
        {
            bool update = newFrame || !_hasNativeFlush;
            if (update)
            {
                // The frozen body pool stops while the absolute-time actor
                // continues. A retained record with no body update (for example
                // a defeated role during results) still uses the frame delta.
                if (_preparedSinceFlush && !_preparedBodyAdvanced)
                    frameDelta = 0;
                SampleBladePoses(0, true, bodyVisible);
                _preparedSinceFlush = false;
                _preparedBodyAdvanced = false;
            }
            if (_weaponBlades.Count == 0 || camera == null)
                return;
            _weaponBladeMaterial ??= WeaponBlade.CreateMaterial();
            for (int flush = 0; flush < 2; flush++)
                foreach (WeaponBlade blade in _weaponBlades.Values)
                {
                    if (update)
                        blade.Advance(frameDelta, _bladeRate, rebuildMesh: false);
                    blade.RenderFlush(camera, _weaponBladeMaterial, GameEntity.Layer, flush, update);
                }
            _hasNativeFlush = true;
        }

        private void DisposeWeaponPresentation()
        {
            foreach (WeaponBlade blade in _weaponBlades.Values)
                blade.Dispose();
            _weaponBlades.Clear();
            _lastWeaponClock = null;
            _preparedSinceFlush = false;
            _preparedBodyAdvanced = false;
            _hasNativeFlush = false;
            if (_weaponBladeMaterial != null)
                _weaponBladeMaterial.Destroy();
            _weaponBladeMaterial = null;
        }
    }
}
