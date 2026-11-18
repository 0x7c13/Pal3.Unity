// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.BattleEditor
{
    using System;
    using System.Linq;
    using System.Collections.Generic;
    using Battle;
    using Battle.Presentation;
    using UnityEditor;
    using UnityEngine;

    public sealed partial class BattleHudView
    {
        private void DrawStateCards(IReadOnlyList<BattleActorSnapshot> actors)
        {
            int index = 0;
            foreach (BattleActorSnapshot actor in actors)
            {
                if (actor.Side != BattleSide.Ally)
                    continue;
                Rect card = _viewport.StateCard(index);
                bool sword = !_layout.IsPal3A && index % 4 == 0;
                Sprite(sword ? "state/StatePanel0" : "state/StatePanel", new Rect(card.x, card.y, 157, 106));
                int identity = BattleData.PlayerIdentity(actor.ActorId);
                if (identity is >= 0 and < 5)
                {
                    float faceX = _layout.IsPal3A ? 33 : sword ? 43 : 33;
                    Color previous = GUI.color;
                    GUI.color *= BattleHudLayout.FaceTint(actor);
                    Sprite(_layout.StateFace(identity), new Rect(card.x + faceX, card.y + 14, 64, _layout.IsPal3A ? 93 : 82));
                    GUI.color = previous;
                    Sprite(_layout.StateName(identity), new Rect(card.x + (sword ? 38 : 24), card.y + 39, 20, _layout.IsPal3A ? 50 : 32));
                }
                else
                    GUI.Label(new Rect(card.x + 8, card.y + 7, 105, 20), ActorName(actor), Small);
                float offset = sword ? 12 : 0;
                DrawVital(card.x + 107 + offset, card.y + 28, actor.Hp, actor.MaxHp, "HP");
                DrawVital(card.x + 116 + offset, card.y + 54, actor.Sp, actor.MaxSp, "GP");
                DrawVital(card.x + 109 + offset, card.y + 82, actor.Mp, actor.MaxMp, "MP");
                if (sword)
                {
                    Rect bar = new(card.x + 12, card.y + 17, 30, 86);
                    Sprite("state/swordbar", bar);
                    float fraction = Mathf.Clamp01((actor.MagicSword?.Energy ?? 0) / 200);
                    if (fraction > 0)
                    {
                        GUI.BeginGroup(new Rect(bar.x, bar.y + bar.height * (1 - fraction), bar.width, bar.height * fraction));
                        try
                        {
                            Sprite("state/sword", new Rect(0, -bar.height * (1 - fraction), bar.width, bar.height));
                        }
                        finally
                        {
                            GUI.EndGroup();
                        }
                    }
                }
                foreach (BattleStatusSnapshot state in actor.States.Where(state => state.LatentRounds == 0))
                {
                    string resource = StateResource(state.State);
                    if (resource == null || BattleHudLayout.StatusRect(state.State) is not { } icon)
                        continue;
                    icon.position += card.position;
                    Color previous = GUI.color;
                    float luminance = BattleHudLayout.StatusLuminance(_battle.SimulationTick * BattleSession.StepSeconds);
                    GUI.color *= new Color(luminance, luminance, luminance, 1);
                    Sprite("state/specstate/" + resource, icon);
                    GUI.color = previous;
                    GUI.Label(icon, new GUIContent("", state.State + (state.Permanent ? "" : $" · {state.RemainingRounds}回合")));
                }
                GUI.Label(card, new GUIContent("", $"{ActorName(actor)} · {actor.Slot} · {actor.Pose}\n精 {actor.Hp}/{actor.MaxHp} · 气 {actor.Sp}/{actor.MaxSp} · 神 {actor.Mp}/{actor.MaxMp}\n状态：" + string.Join("、", actor.States.Select(state => state.State.ToString()))));
                RegisterControl("inspect:" + actor.Id, card);
                index++;
            }
        }

        private void DrawVital(float x, float y, int value, int maximum, string name)
        {
            Sprite("state/ProgBar", new Rect(x, y, 85, 12));
            float fraction = maximum > 0 ? Mathf.Clamp01(value / (float)maximum) : 0;
            if (fraction > 0)
                Sprite("state/ProgBar_" + name, BattleHudCanvas.ProgressFill(new Rect(x, y, 85, 12), fraction, false));
            BattleHudCanvas.DrawVitalNumber(new Rect(x, y, 85, 12), value, name);
        }

        private void DrawSequence(IReadOnlyList<BattleActorSnapshot> actors)
        {
            float extraWidth = _viewport.CanvasSize.x - 800;
            Sprite("AttackSequen/Back", new Rect(_layout.AttackSequenceX + extraWidth, _layout.AttackSequenceY, 320, 32));
            foreach (BattleActorSnapshot actor in actors)
            {
                if (!actor.Alive || actor.Escaped)
                    continue;
                Vector2 point = _layout.AttackPortrait(actor.Gauge, actor.CastGauge, actor.Pose is BattlePose.Casting or BattlePose.Fleeing);
                point.x += extraWidth;
                Rect rect = new(point.x, point.y, 64, 64);
                string model = _preview.ActorModel(actor.Id);
                if (model != null && !_sequenceSprites.TryGetValue(model, out _))
                {
                    string path = $"basedata.cpk\\ui\\combat\\attacksequen\\{model}.tga";
                    _sequenceSprites[model] = _data.Resources.Find(path) != null ? "attacksequen/" + model : null;
                }
                if (model != null && _sequenceSprites[model] is { } sprite)
                    Sprite(sprite, rect);
                else
                    GUI.Label(new Rect(point.x + 8, point.y + 9, 48, 28), ActorName(actor), Small);
                GUI.Label(rect, new GUIContent("", $"{ActorName(actor)} · 第一槽 {actor.Gauge}/{BattleSession.FullGauge} · 第二槽 {actor.CastGauge}/{BattleSession.FullCastGauge}"));
                RegisterControl("inspect:" + actor.Id, rect);
            }
        }

        private static string StateResource(BattleState state) => state switch
        {
            BattleState.AttackIncrease => "attackinc",
            BattleState.AttackDecrease => "attackdec",
            BattleState.DefenseIncrease => "defenceinc",
            BattleState.DefenseDecrease => "defencedec",
            BattleState.SpeedIncrease => "speedinc",
            BattleState.SpeedDecrease => "speeddec",
            BattleState.LuckIncrease => "luckinc",
            BattleState.LuckDecrease => "luckdec",
            BattleState.Paralysis => "stable",
            BattleState.Seal => "blank",
            BattleState.Forbidden => "forbid",
            BattleState.Sleep => "sleep",
            BattleState.Chaos => "chaos",
            BattleState.Madness => "mad",
            BattleState.Reflection => "mirror",
            BattleState.Evade => "wall",
            BattleState.Barrier => "bound",
            BattleState.Invisible => "hermit",
            BattleState.PoisonResist => "immunity",
            _ => null
        };
    }
}
