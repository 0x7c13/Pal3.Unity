// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.BattleEditor
{
    using System;
    using System.Linq;
    using Battle;
    using Battle.Presentation;
    using UnityEditor;
    using UnityEngine;

    public sealed partial class BattleHudView
    {
        private void DrawFormation()
        {
            Vector2 origin = new(19, 232);
            Sprite("control/Lineup/back", new Rect(origin.x, origin.y, 230, 250));
            GUI.Label(new Rect(19, 192, 320, 36), "阵法 · 拖动头像换位\n右键或确认结束；原阵型不耗条", Small);
            foreach (BattleSlot slot in Enum.GetValues(typeof(BattleSlot)))
            {
                Vector2 point = origin + BattleHudLayout.FormationPosition(slot);
                Rect drop = new(point.x, point.y, 40, 38);
                RegisterControl("slot:" + (int)slot, drop);
                if (_presenter.CapturedPointerId.HasValue && _presenter.FormationActor != null && drop.Contains(_pointer))
                    EditorGUI.DrawRect(drop, new Color(1, 1, 0, .2f));
            }
            foreach (BattleActorSnapshot actor in _battle.Actors.Where(actor => actor.Side == BattleSide.Ally))
            {
                Vector2 point = origin + BattleHudLayout.FormationPosition(actor.Slot);
                Rect rect = new(point.x, point.y, 40, 38);
                RegisterControl("actor:" + actor.Id, rect);
                int identity = BattleData.PlayerIdentity(actor.ActorId);
                bool enabled = _battle.CanSwapFormation(actor.Id, actor.Slot) == null;
                if (identity is >= 0 and < 5)
                {
                    Color saved = GUI.color;
                    if (_navigationInput && _presenter.FocusedId == "actor:" + actor.Id)
                        GUI.color *= new Color(1, 1, 1, .65f + .35f * Mathf.Sin((float)UnityEditor.EditorApplication.timeSinceStartup * 7.8f));
                    Sprite(_layout.FormationFace(identity, !enabled), rect);
                    GUI.color = saved;
                }
                else
                    GUI.Label(rect, ActorName(actor), Small);
            }
            if (_navigationInput && _presenter.FormationActor != null && _presenter.FocusedId?.StartsWith("slot:", StringComparison.Ordinal) == true)
            {
                BattleActorSnapshot moving = _battle.Actors.First(actor => actor.Id == _presenter.FormationActor);
                int identity = BattleData.PlayerIdentity(moving.ActorId);
                BattleSlot slot = (BattleSlot)int.Parse(_presenter.FocusedId[5..]);
                Color saved = GUI.color;
                GUI.color *= new Color(1, 1, 1, .65f);
                if (identity is >= 0 and < 5)
                    Sprite(_layout.FormationFace(identity, false), new Rect(origin + BattleHudLayout.FormationPosition(slot), new Vector2(40, 38)));
                GUI.color = saved;
            }
            TextButton(new Rect(80, 460, 100, 20), "确认阵型", controlId: "end");
        }

        private static readonly Rect AiPanelBounds = new(24, 316, 158, 158);

        private void DrawAi()
        {
            BattleActorSnapshot[] actors = _battle.Actors.ToArray();
            BattleActorSnapshot selected = actors.FirstOrDefault(actor => actor.Id == _presenter.AiActor) ?? actors.First();
            Sprite("control/AI/back", AiPanelBounds);
            int identity = BattleData.PlayerIdentity(selected.ActorId);
            if (identity is >= 0 and < 5)
                Sprite(_layout.AiName(identity), new Rect(81, 347, 38, 16));
            else
                GUI.Label(new Rect(35, 343, 130, 20), ActorName(selected), Small);
            int flags = _presenter.AiFlags(selected.Id);
            string[] sprites = { "attack", "magic", "skill", "item", "affirmative", "ai" };
            string[] labels = { "普攻", "仙术", "特技", "物品", "全力／万全", "自动" };
            Vector2[] offsets = { new(7, 48), new(78, 73), new(7, 73), new(78, 48), new(7, 98), new(78, 98) };
            for (int bit = 0; bit < sprites.Length; bit++)
            {
                Vector2 at = new Vector2(24, 316) + offsets[bit];
                SpriteButton("control/AI/" + sprites[bit], new Rect(at.x, at.y, 73, 25), selected: bit == 4 ? (flags & 16) == 0 : (flags & 1 << bit) != 0,
                    tooltip: labels[bit], disabledImage: false);
            }
            SpriteButton("control/AI/ok", new Rect(59, 443, 92, 26));

        }
    }
}
