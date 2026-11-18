// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.BattleEditor
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Battle;
    using Core.DataReader.Pol;
    using Game.Rendering.Renderer;
    using UnityEngine;
    using Object = UnityEngine.Object;

    public sealed partial class BattlePreview
    {
        private sealed class ThrownWeaponView
        {
            internal int Item;
            internal GameObject Object;
            internal WeaponBlade Blade;
            internal double Clock;
            internal bool Active;
#if PAL3
            internal bool HasNativeFlush;
#endif
        }

        private readonly Dictionary<string, ThrownWeaponView> _thrownWeapons = new(StringComparer.Ordinal);
        private Material _thrownBladeMaterial;

        // Native stack: Translate, RotateY, RotateX, RotateZ. Mirroring X at the
        // rendering boundary reverses Y and Z rotation, retaining the X angle.
        public static Quaternion ThrownWeaponRotation(BattleMovementPoint degrees) =>
            Quaternion.AngleAxis(-degrees.Y, Vector3.up) * Quaternion.AngleAxis(degrees.X, Vector3.right) *
            Quaternion.AngleAxis(-degrees.Z, Vector3.forward);

        private void ApplyThrownWeapons(BattleSession combat)
        {
            var retained = new HashSet<string>(StringComparer.Ordinal);
            foreach (BattleThrownWeaponSnapshot projectile in combat.ThrownWeapons)
            {
                retained.Add(projectile.Actor);
                BattleActorSnapshot source = combat.Actors.First(actor => actor.Id == projectile.Actor);
                BattleParticipant participant = combat.CaptureParticipant(projectile.Actor);
                int weapon = participant.Equipment[0];
                if (_thrownWeapons.TryGetValue(projectile.Actor, out ThrownWeaponView view) && view.Item != weapon)
                {
                    ReleaseThrownWeapon(view);
                    _thrownWeapons.Remove(projectile.Actor);
                    view = null;
                }
                if (view == null)
                {
                    var item = _data.Database.GameItemInfos[weapon];
                    string path = $"basedata.cpk\\item\\{item.ModelName}\\{item.ModelName}.pol";
                    PolFile model = _data.Read(path, new PolFileReader());
                    var candidate = new ThrownWeaponView
                    {
                        Item = weapon,
                        Object = View.CreateObject("Thrown weapon " + projectile.Actor),
                        Blade = new WeaponBlade(model.TagNodes),
                        Clock = source.NativePresentationTick * BattleSession.StepSeconds
                    };
                    try
                    {
                        View.AddPolyModel(candidate.Object, path, model, item.ModelName + item.ImageId + ".tga");
                    }
                    catch
                    {
                        ReleaseThrownWeapon(candidate);
                        throw;
                    }
                    _thrownWeapons.Add(projectile.Actor, view = candidate);
                }
                view.Object.transform.SetPositionAndRotation(BattleAnimationCatalog.UnityPosition(projectile.Position),
                    ThrownWeaponRotation(projectile.RotationDegrees));
                foreach (UnityEngine.Renderer renderer in view.Object.GetComponentsInChildren<UnityEngine.Renderer>(true))
                    renderer.enabled = projectile.Active;
                double clock = source.NativePresentationTick * BattleSession.StepSeconds;
                // The secondary POL retains BladeRecord's default time scale 1.
                view.Active = projectile.Active;
#if PAL3A
                view.Blade.Sample(view.Object.transform, projectile.Active, (float)Math.Max(0, clock - view.Clock), 1, .12f);
#endif
                view.Clock = clock;
            }
            foreach (string id in _thrownWeapons.Keys.Where(id => !retained.Contains(id)).ToArray())
            {
                ReleaseThrownWeapon(_thrownWeapons[id]);
                _thrownWeapons.Remove(id);
            }
        }

        private void RenderThrownWeaponBlades(Camera camera, float delta, bool newFrame)
        {
            if (_thrownWeapons.Count == 0)
                return;
            _thrownBladeMaterial ??= WeaponBlade.CreateMaterial();
            foreach (ThrownWeaponView view in _thrownWeapons.Values)
            {
#if PAL3
                bool update = newFrame || !view.HasNativeFlush;
                if (update)
                    view.Blade.SamplePose(view.Object.transform, view.Active, .12f, rebuildMesh: false);
                for (int flush = 0; flush < 2; flush++)
                {
                    if (update)
                        view.Blade.Advance(delta, 1, rebuildMesh: false);
                    view.Blade.RenderFlush(camera, _thrownBladeMaterial, view.Object.layer, flush, update);
                }
                view.HasNativeFlush = true;
#else
                view.Blade.Render(camera, _thrownBladeMaterial, view.Object.layer);
#endif
            }
        }

        private void ReleaseThrownWeapon(ThrownWeaponView view)
        {
            view.Blade.Dispose();
            View.ReleaseCompositeObject(view.Object);
        }

        private void DisposeThrownWeapons()
        {
            foreach (ThrownWeaponView view in _thrownWeapons.Values)
                ReleaseThrownWeapon(view);
            _thrownWeapons.Clear();
            if (_thrownBladeMaterial != null)
                Object.DestroyImmediate(_thrownBladeMaterial);
            _thrownBladeMaterial = null;
        }
    }
}
