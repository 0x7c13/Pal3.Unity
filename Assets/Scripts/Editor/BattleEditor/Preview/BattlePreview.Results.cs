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
    using Core.Contract.Enums;
    using Game.Rendering.Native;
    using UnityEditor;
    using UnityEngine;

    public sealed partial class BattlePreview : IBattleResultBodyPort
    {
        private readonly Dictionary<string, BattleResultRoleBody> _resultBodies = new(StringComparer.Ordinal);
        private BattleSession _resultBattle;
        private int _resultStep = -1, _musicStops;
        private double _resultStart;

        public void BeginResultPresentation(BattleSession combat, BattleAnimationCatalog catalog, BattleResultPresentation result)
        {
            _resultBattle = combat;
            _resultStart = combat.Tick * BattleSession.StepSeconds;
            _resultStep = -1;
            _musicStops = 0;
            _resultBodies.Clear();
            Camera?.StartList(result.CameraList);
            foreach (BattleResultStep award in result.Sequence.Where(step => step.Kind == BattleResultStepKind.Item))
                Audio.Play("wa006");
            bool victory = combat.Result.Winner == BattleSide.Ally && !combat.Result.Escaped;
            var beforeSettlement = combat.Result.PreSettlementActors.ToDictionary(actor => actor.Id, StringComparer.Ordinal);
            foreach (BattleActorSnapshot actor in result.Actors.Where(actor => actor.Side == BattleSide.Ally))
            {
                ActorView view = BattleActorView(combat, actor);
                // Settlement preserves actor IDs. Use the presentation snapshot
                // as a fallback when the caller omits a pre-settlement actor.
                BattleActorSnapshot before = beforeSettlement.GetValueOrDefault(actor.Id) ?? actor;
                var state = new BattleResultBodyState(actor.Hp, actor.MaxHp, actor.Guarding,
                    actor.States.Any(status => status.State == BattleState.Sleep), actor.States.Any(status => status.State == BattleState.Paralysis));
                bool winAction = victory && (BattleData.Variant == "PAL3A" || before.AliveFlag);
                _resultBodies.Add(actor.Id, new BattleResultRoleBody(BattleData.Variant, combat.CaptureBodyAnimation(actor.Id),
                    catalog.ResultBodyAnimations(view.Actor), state, winAction));
                if (winAction)
                    view.Object.transform.rotation = Quaternion.Euler(0, -BattleFacingRules.Initial(actor.Side), 0);
            }
            ApplyResultPresentation(result, false);
        }

        public void PrepareResultBodyPass()
        {
            foreach (BattleResultRoleBody body in _resultBodies.Values)
                body.PrepareStagePass();
        }

        public bool TryStartLevelUp(string actor) => _resultBodies.TryGetValue(actor, out BattleResultRoleBody body) && body.TryStartLevelUp();

        public void ApplyResultPresentation(BattleResultPresentation result, bool advance)
        {
            Time = (float)(_resultStart + result.Tick * BattleSession.StepSeconds);
            SetBladeFrameTime(_resultStart + result.Tick * BattleSession.StepSeconds);
            Audio.SetTime(Time);
            if (advance)
            {
                Camera?.Advance(BattleSession.StepSeconds, BattleSession.StepSeconds);
                Trail.Advance(BattleSession.StepSeconds, BattleSession.StepSeconds);
                foreach (var entry in _resultBodies)
                {
                    entry.Value.Step();
                    if (!_actors.TryGetValue(entry.Key, out ActorView actor))
                        continue;
                    BattleBodyAnimationBinding binding = entry.Value.Binding;
                    string clip = binding.CurrentAnimation?.Clip;
                    if (clip == null)
                        continue;
                    if (actor.Action != clip)
                        SetAction(actor, clip);
                    actor.Renderer.SampleAnimation((float)binding.CurrentAnimationTimeSeconds);
                    BattleWeaponVisibility visibility = ResultWeaponVisibility(entry.Key, binding);
                    actor.Renderer.SetWeaponVisibility(actor.Visible && visibility.Left, actor.Visible && visibility.Right);
                    actor.Renderer.SampleWeaponBlades(Time, entry.Value.PlaybackRate,
                        entry.Value.BladeSeconds);
                }
            }
            ApplyResultActors(result.Actors);
            Audio.SetMusicGain(result.MusicGain);
            if (result.MusicStopCount != _musicStops)
            {
                Audio.StopMusic();
                _musicStops = result.MusicStopCount;
            }
            if (_resultStep != result.Index && result.HasExecuted)
            {
                _resultStep = result.Index;
                string sound = result.Current?.Kind switch
                {
                    BattleResultStepKind.WinBanner => "wa012",
                    BattleResultStepKind.LoseBanner => "wa013",
                    BattleResultStepKind.LevelUp => "wa014",
                    BattleResultStepKind.SkillLearned or BattleResultStepKind.SkillImproved => "wa015",
                    _ => null
                };
                if (sound != null)
                    Audio.Play(sound);
            }
        }

        private void ApplyResultActors(IReadOnlyList<BattleActorSnapshot> actors)
        {
            _shadows?.Begin();
            foreach (BattleActorSnapshot state in actors)
            {
                ActorView actor = BattleActorView(_resultBattle, state);
                actor.Visible = actor.Action != null && !state.Escaped && _resultBattle.CoAttackActorVisible(state.Id) &&
                    (state.ModelAliveFlag || state.Side == BattleSide.Ally && state.Hp > 0);
                foreach (UnityEngine.Renderer renderer in actor.Renderers)
                    renderer.enabled = actor.Visible;
                BattleWeaponVisibility weapons = _resultBodies.TryGetValue(state.Id, out BattleResultRoleBody body) ?
                    ResultWeaponVisibility(state.Id, body.Binding) : _resultBattle.WeaponVisibility(state.Id);
                actor.Renderer.SetWeaponVisibility(actor.Visible && weapons.Left, actor.Visible && weapons.Right);
                float blend = state.Movement?.Blend ?? (state.States.Any(status => status.State == BattleState.Invisible) ? .44f : 1f);
                BattleEffectActorSnapshot effect = _resultBattle.EffectActor(state.Id);
                if (effect?.HasBlend == true)
                    blend = effect.Blend;
                actor.Renderer.SetOpacity(blend);
                if (_shadows != null && GameBoxBattleShadow.IsVisible(BattleData.Variant == "PAL3A", true, state.ModelAliveFlag,
                    state.Side == BattleSide.Ally, state.Hp, blend))
                    _shadows.Add(new GameBoxBattleShadow(actor.Object.transform.position, _shadowRadii[state.Id]));
            }
            _shadows?.End();
        }

        private BattleWeaponVisibility ResultWeaponVisibility(string actorId, BattleBodyAnimationBinding binding)
        {
            if (_resultBattle.Definitions?.Actors.TryGetValue(actorId, out BattleActorDefinition definition) != true)
                return _resultBattle.WeaponVisibility(actorId);
            BattleEquipmentDefinition weapon = definition.Equipment.FirstOrDefault(item => item.Slot == 0);
            return BattleWeaponPresentationRules.Visible(BattleData.Variant, definition.Identity,
                binding.RequestedNativeActionIndex, weapon?.WeaponType ?? 0,
                _resultBattle.Pal3AForm(actorId).Kind != BattlePal3AFormKind.Human,
                weapon != null && _resultBattle.WeaponDrawingEnabled(actorId));
        }

        private Texture2D ResultTexture(string name) => View.GetTexture("basedata.cpk\\ui\\combat\\result\\" + name + ".tga");

        private void ResultSprite(string name, Rect rectangle)
        {
            Texture2D texture = ResultTexture(name);
            GUI.DrawTextureWithTexCoords(rectangle, texture, BattleHudCanvas.SpriteUv(texture, rectangle.size), true);
        }

        public void DrawResultOverlay(BattleResultPresentation result, Rect viewport, Vector4 safeInsets = default)
        {
            if (_resultBattle == null || result == null || result.Completed)
                return;
            Rect reference = new BattleHudViewport(viewport, safeInsets).ReferenceRect;
            if (viewport.width < 16 || viewport.height < 16)
                return;
            using (new BattleHudCanvas(new BattleHudViewport(reference, default)))
            {
                if (result.StatisticsVisible)
                    DrawResultStatistics(result);
                BattleResultStep step = result.HasExecuted ? result.Current : null;
                if (step?.Kind is BattleResultStepKind.WinBanner or BattleResultStepKind.LoseBanner)
                    GUI.DrawTexture(new Rect(150, -50, 256, 512), ResultTexture(step.Kind == BattleResultStepKind.WinBanner ? "bigwin" : "biglose"), ScaleMode.StretchToFill, true);
                else if (step?.Kind is BattleResultStepKind.LevelUp or BattleResultStepKind.SkillLearned or BattleResultStepKind.SkillImproved or BattleResultStepKind.Money or BattleResultStepKind.Item)
                    DrawResultPrompt(step);
            }
            // A scene transition covers the rendered viewport, including areas
            // outside the fitted original UI canvas and its safe insets.
            if (result.ScreenOpacity > 0)
                EditorGUI.DrawRect(viewport, new Color(0, 0, 0, result.ScreenOpacity));
        }

        private void DrawResultStatistics(BattleResultPresentation presentation)
        {
            BattleResult result = _resultBattle.Result;
            int index = 0;
            foreach (BattleActorSnapshot actor in presentation.Actors.Where(actor => actor.Side == BattleSide.Ally).OrderBy(actor => actor.NativeIndex))
            {
                int y = BattleResultStatistics.CardY(index, presentation.StatisticsSeconds);
                using (new GUI.GroupScope(new Rect(index * 200, y, 256, 256)))
                {
                    ResultSprite("back", new Rect(0, 0, 256, 256));
                    int identity = BattleData.PlayerIdentity(actor.ActorId);
                    Color faceColor = GUI.color;
                    try
                    {
                        GUI.color *= BattleHudLayout.FaceTint(actor, false);
                        ResultSprite("face/" + identity, new Rect(15, -15, 64, 128));
                    }
                    finally
                    {
                        GUI.color = faceColor;
                    }
                    ResultSprite("face/HP-GP-MP", new Rect(66, 42, 64, 67));
                    DrawResultBar(new Rect(83, 46, 85, 12), actor.Hp, actor.MaxHp, "HP");
                    DrawResultBar(new Rect(86, 71, 85, 12), actor.Sp, actor.MaxSp, "GP");
                    DrawResultBar(new Rect(84, 97, 85, 12), actor.Mp, actor.MaxMp, "MP");
                    string name = _data.Database.CombatActorInfos[actor.ActorId].Name;
                    GUI.Label(new Rect(0, 0, 187, 32), $"{name} {actor.Level} 级", ResultText);
                    BattleActorReward reward = result.Rewards.FirstOrDefault(reward => reward.Actor == actor.Id);
                    int exp = BattleResultStatistics.Experience(reward?.ExperienceGained ?? 0, presentation.StatisticsSeconds);
                    GUI.Label(new Rect(0, 211, 187, 32), exp.ToString(), ResultText);
                    BattleActorSnapshot before = result.PreSettlementActors.FirstOrDefault(candidate => candidate.Id == actor.Id) ?? actor;
                    BattleDefinitions definitions = _resultBattle.Definitions;
                    IReadOnlyList<int> uses = definitions == null ? null : BattleResultStatistics.MagicUseCounts(_resultBattle.Variant, definitions,
                        definitions.Actors.GetValueOrDefault(actor.Id)?.Identity ?? identity, before.Skills);
                    foreach ((int group, float x, float row) in new[] { (0, 51f, 108f), (1, 136f, 108f), (2, 51f, 134f), (3, 136f, 134f), (4, 51f, 160f), (5, 136f, 160f) })
                        GUI.Label(new Rect(x, row, 64, 32), (uses?[group] ?? 0).ToString(), ResultText);
                }
                index++;
            }
        }

        private void DrawResultBar(Rect rect, int value, int maximum, string texture)
        {
            ResultSprite("face/SLOT", rect);
            float fraction = maximum > 0 ? Math.Clamp(value / (float)maximum, 0, 1) : 0;
            if (fraction > 0)
                ResultSprite("face/" + texture, BattleHudCanvas.ProgressFill(rect, fraction, true));
            BattleHudCanvas.DrawVitalNumber(rect, value, texture);
        }

        private void DrawResultPrompt(BattleResultStep step)
        {
            string actor = step.Actor == null ? "" : _data.Database.CombatActorInfos[_resultBattle.Result.Actors.First(candidate => candidate.Id == step.Actor).ActorId].Name;
            string panel = "levelup";
            string ability = _resultBattle.Definitions.Skills.TryGetValue(step.Id, out BattleAbilityDefinition skill) ? skill.Name : step.Id.ToString();
            string text = step.Kind switch
            {
                BattleResultStepKind.LevelUp => $"{actor}升到 {step.Value} 级",
                BattleResultStepKind.SkillLearned => $"{actor}学会 {ability}",
                BattleResultStepKind.SkillImproved => $"{actor} {ability} 升到 {step.Value} 级",
                BattleResultStepKind.Money => $"得到 {step.Value} 文钱",
                BattleResultStepKind.Item => $"得到 {_data.Database.GameItemInfos[step.Id].Name}" + (step.Value == 1 ? "" : " " + step.Value),
                _ => ""
            };
            Vector2 size = new(233, 64);
            if (step.Kind is BattleResultStepKind.Money or BattleResultStepKind.Item)
            {
                panel = "getitem";
                size = new Vector2(359, 91);
            }
            else if (skill?.IsMagic == true)
            {
                panel = skill.ElementMask switch
                {
                    1 => "waterlvup",
                    2 => "firelvup",
                    4 => "windlvup",
                    8 => "thunderlvup",
                    16 => "earthlvup",
                    _ => "doublelvup"
                };
                size = new Vector2(302, 75);
            }
            Rect bounds = new((800 - size.x) / 2, (600 - size.y) / 2, size.x, size.y);
            ResultSprite("panel/" + panel, bounds);
            GUI.Label(bounds, text, ResultText);
        }

        private static GUIStyle ResultText => BattleHudCanvas.LabelStyle(14, Color.white, alignment: TextAnchor.MiddleCenter);
    }
}
