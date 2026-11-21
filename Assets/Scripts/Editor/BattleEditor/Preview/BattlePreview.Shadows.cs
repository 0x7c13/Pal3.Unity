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
    using Core.DataReader.Mv3;
    using Game.Rendering.Native;
    using UnityEngine;

    public sealed partial class BattlePreview
    {
        private readonly Dictionary<string, float> _shadowRadii = new(StringComparer.Ordinal);
        private GameBoxBattleShadowRenderer _shadows;
        private double _bladeFrameTime;
        private double? _lastBladeRenderTime;

        // A fixed-step presentation frame is stable across inspector repaints,
        // captures and paused playback; rendering never consumes a gameplay step.
        private void SetBladeFrameTime(double time) => _bladeFrameTime = time;

        private void InitializeShadows()
        {
            // Toon uses the body's ShadowCaster pass and the preview key light.
            // Native blob shadows and its post-shadow material queues belong
            // exclusively to the original rendering mode.
            if (_scenario.ToonRendering)
                return;
            _shadows = new GameBoxBattleShadowRenderer(BattleData.Variant, View.GetTexture("basedata.cpk\\shadow.tga"));
            RefreshShadowRadii(true);
            View.Rendering += RenderShadows;
        }

        private void RefreshShadowRadii(bool initial = false)
        {
            foreach (ActorView actor in _actors.Values)
                _shadowRadii[actor.Actor.InstanceId] = GameBoxBattleShadow.CaptureRadius(initial ? InitialShadowModel(actor) : actor.Model);
        }

        private Mv3File InitialShadowModel(ActorView actor)
        {
            // Released PAL3A Role.Init selects an already transformed body
            // before Stage.Init captures its shadow bounds.
            string model = BattleData.Variant != "PAL3A" ? null :
                actor.Actor.ActorId == 101 && _scenario.Pal3A.NangongStartsTransformed ? "107" :
                actor.Actor.ActorId == 103 && _scenario.Pal3A.PengxuStartsTransformed ? "106" : null;
            if (model == null)
                return actor.Model;
            BattleParticipant participant = actor.Actor.Copy();
            participant.Model = model;
            string path = _data.ActionPath(participant, _data.IdleAction(participant));
            return ReadActorModel(path);
        }

        private void RefreshShadowsForActorReplacement(ActorView previous, ActorView candidate)
        {
            // Published NewMonster reinitializes every slot. PAL3A form changes
            // only replace the body and retain the existing shadow radius.
            if (_shadows != null && (previous == null || previous.Generation != candidate.Generation))
                RefreshShadowRadii();
        }

        private void SampleShadows()
        {
            if (_shadows == null)
                return;
            _shadows.Begin();
            foreach (ActorView actor in _actors.Values)
                _shadows.Add(new GameBoxBattleShadow(actor.Object.transform.position, _shadowRadii[actor.Actor.InstanceId]));
            _shadows.End();
        }

        private void ApplyShadows(BattleSession combat)
        {
            if (_shadows == null)
                return;
            _shadows.Begin();
            foreach (BattleActorSnapshot actor in combat.Actors)
            {
                float blend = actor.Movement?.Blend ?? (actor.States.Any(status => status.State == BattleState.Invisible) ? .44f : 1f);
                BattleEffectActorSnapshot effect = combat.EffectActor(actor.Id);
                if (effect?.HasBlend == true)
                    blend = effect.Blend;
                if (GameBoxBattleShadow.IsVisible(BattleData.Variant == "PAL3A", true, actor.ModelAliveFlag,
                    actor.Side == BattleSide.Ally, actor.Hp, blend))
                    _shadows.Add(new GameBoxBattleShadow(BattleAnimationCatalog.UnityPosition(actor.Position), _shadowRadii[actor.Id]));
            }
            _shadows.End();
        }

        private static void SetShadowRenderOrder(GameObject root, int order)
        {
            GameBoxBattleRenderPhase.Apply(root, order);
        }

        private void RenderShadows(Camera camera)
        {
            bool pal3A = BattleData.Variant == "PAL3A";
            View.SetEnvironmentRenderOrder(GameBoxBattleRenderOrder.BeforeShadows);
            foreach (ActorView actor in _actors.Values)
                SetShadowRenderOrder(actor.Object, GameBoxBattleRenderOrder.Actor(pal3A, actor.Actor.Side == BattleSide.Ally, actor.Actor.ActorId));
            foreach (var pair in _thrownWeapons)
                if (_actors.TryGetValue(pair.Key, out ActorView actor))
                    SetShadowRenderOrder(pair.Value.Object, GameBoxBattleRenderOrder.Actor(pal3A, actor.Actor.Side == BattleSide.Ally, actor.Actor.ActorId));
            foreach (GameObject corpse in _corpses.Values)
                SetShadowRenderOrder(corpse, GameBoxBattleRenderOrder.BeforeShadows);
            _shadows?.Render(camera, View.Root.layer);
        }

        private void DisposeShadows()
        {
            if (View != null)
                View.Rendering -= RenderShadows;
            _shadows?.Dispose();
            _shadows = null;
        }
    }
}
