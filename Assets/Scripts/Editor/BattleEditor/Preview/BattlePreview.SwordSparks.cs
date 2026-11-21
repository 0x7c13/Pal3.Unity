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
    using Battle.Presentation;
    using UnityEngine;
    using UnityEngine.Rendering;
    using Object = UnityEngine.Object;

    public sealed partial class BattlePreview
    {
        private BattleSession _swordSparkOwner;
        private BattleSwordSparkProjection _swordSparks;
        private long _swordSparkTick;
        private Material _swordSparkMaterial;

        public IReadOnlyList<BattleSwordSparkFrame> SwordSparkFrames => _swordSparks?.Frames ?? Array.Empty<BattleSwordSparkFrame>();

        private void ApplySwordSparks(BattleSession battle)
        {
            if (battle.Definitions.Variant != "PAL3" || battle.Status != BattleSessionStatus.Running)
            {
                _swordSparks?.Clear();
                return;
            }
            if (_swordSparkOwner != battle || battle.SimulationTick < _swordSparkTick)
            {
                _swordSparkOwner = battle;
                _swordSparks = new BattleSwordSparkProjection(_scenario.Seed);
                _swordSparkTick = 0;
            }
            long elapsed = battle.SimulationTick - _swordSparkTick;
            _swordSparkTick = battle.SimulationTick;
            if (elapsed == 0)
                return;
            BattleActorSnapshot actor = battle.Actors.FirstOrDefault(actor => actor.Side == BattleSide.Ally &&
                actor.NativeIndex == 0 && battle.Definitions.Actors[actor.Id].Identity == 0);
            bool visible = actor?.Alive == true && !battle.IsModalPresentation;
            _swordSparks.Advance(elapsed * BattleSession.StepSeconds, visible, actor?.MagicSword?.Energy ?? 0);
        }

        public void DrawSwordSparks(Rect viewport, Vector4 safeInsets = default)
        {
            if (_swordSparkOwner?.Status != BattleSessionStatus.Running || _swordSparkOwner.IsModalPresentation ||
                Event.current.type != EventType.Repaint || viewport.width < 1 || viewport.height < 1)
                return;
            IReadOnlyList<BattleSwordSparkFrame> frames = SwordSparkFrames;
            if (frames.Count == 0)
                return;
            if (_swordSparkMaterial == null)
            {
                Shader shader = Shader.Find("Pal3/Effect") ?? throw new InvalidOperationException("The original effect shader is missing.");
                _swordSparkMaterial = new Material(shader)
                {
                    name = "Original magic sword HUD stars",
                    hideFlags = HideFlags.HideAndDontSave
                };
                _swordSparkMaterial.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
                _swordSparkMaterial.SetInt("_DstBlend", (int)BlendMode.One);
                _swordSparkMaterial.SetInt("_ZTest", (int)CompareFunction.Always);
                _swordSparkMaterial.SetInt("_Cull", (int)CullMode.Off);
            }
            Texture2D texture = View.GetTexture("basedata.cpk\\effect\\flareBlue3.bmp");
            var context = new BattleHudViewport(viewport, safeInsets);
            using (new BattleHudCanvas(context))
            {
                float offsetY = context.StateCard(0).y - BattleHudLayout.StateCard(0).y;
                foreach (BattleSwordSparkFrame frame in frames)
                    Graphics.DrawTexture(new Rect(frame.X, frame.Y + offsetY, frame.Size, frame.Size), texture,
                        new Rect(0, 0, 1, 1), 0, 0, 0, 0, new Color32(255, 255, 255, frame.Alpha), _swordSparkMaterial);
            }
        }

        private void DisposeSwordSparks()
        {
            if (_swordSparkMaterial != null)
                Object.DestroyImmediate(_swordSparkMaterial);
            _swordSparkMaterial = null;
            _swordSparks?.Clear();
            _swordSparks = null;
            _swordSparkOwner = null;
            _swordSparkTick = 0;
        }
    }
}
