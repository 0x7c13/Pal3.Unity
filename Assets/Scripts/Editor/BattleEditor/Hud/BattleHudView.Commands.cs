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
        private void DrawCommands()
        {
            Sprite("control/back", new Rect(10, 225, 256, 256));
            BattleActorSnapshot actor = _battle.Actors.First(value => value.Id == _presenter.Controller);
            // CCBControlWindow creates the sword before the ordinary controls;
            // the overlapping formation/item buttons retain their top layer.
            if (!_layout.IsPal3A && actor.MagicSword is { SkillId: > 0 } sword && sword.Energy > 199.9f)
                SpriteButton("control/mj", new Rect(0, 410, 84, 84), tooltip: "魔剑技", disabledImage: false);
            SpriteButton("control/attack", new Rect(63, 381, 46, 46), tooltip: "击 · 普通攻击");
            SpriteButton("control/skill", new Rect(67, 322, 46, 46), tooltip: "技 · 特技");
            SpriteButton("control/magic", new Rect(12, 378, 46, 46), tooltip: "术 · 仙术");
            SpriteButton("control/item", new Rect(66, 433, 46, 46), tooltip: "物 · 恢复／攻击／仙宝");
            SpriteButton("control/defence", new Rect(121, 378, 46, 46), tooltip: "防 · 防御");
            SpriteButton("control/flee", new Rect(100, 417, 46, 46), tooltip: "逃 · 等待逃跑");
            SpriteButton("control/lineup", new Rect(29, 417, 46, 46), tooltip: "阵 · 拖动角色换位");
            SpriteButton("control/ai", new Rect(29, 346, 46, 46), tooltip: "AI · 战斗策略");
            SpriteButton("control/protect", new Rect(100, 346, 46, 46), tooltip: "护 · 援护对象");
        }

        public static bool ElementWheelVisible(BattleSession combat, BattleHudPresenter presenter) => combat != null && combat.Definitions?.Variant == "PAL3A" &&
            combat.Status == BattleSessionStatus.Running && presenter.Page == BattleHudPage.Commands && presenter.Controller != null &&
            combat.Actors.First(actor => actor.Id == presenter.Controller).NativeIndex == 0;

        private void DrawWheelControls()
        {
            if (!ElementWheelVisible(_battle, _presenter))
                return;
            for (int core = 0; core < 5; core++)
            {
                int skill = core switch
                {
                    2 => 267,
                    3 => 266,
                    _ => 264 + core
                };
                BattleSpiritRect native = BattlePreview.SpiritWheelTarget(core);
                Vector2 position = _viewport.ToCanvas(_viewport.NativeToViewport(new Vector2(native.Left, native.Top)));
                Rect rect = new(position.x, position.y, native.Right - native.Left, native.Bottom - native.Top);
                string id = "wheel:" + skill;
                int ui = core switch
                {
                    2 => 3,
                    3 => 2,
                    _ => core
                };
                int tier = _battle.Pal3AState.SpiritWheel.Charges[core] / 3;
                string image = (tier < 3 ? "unwt" : "wt") + (4 - ui) + (tier < 3 ? tier : 0);
                RegisterControl(id, rect, SpriteTexture("control/" + image));
                GUI.Label(rect, new GUIContent("", _battle.Definitions.Skills.TryGetValue(skill, out BattleAbilityDefinition ability) ? ability.Name + " · " + (rect.Contains(_pointer) ? _presenter.WheelReason(skill) ?? "五灵轮" : "五灵轮") : "五灵轮"));
            }
            Vector2 center = _viewport.ToCanvas(_viewport.NativeToViewport(new Vector2(739, 28)));
            Rect beast = new(center.x, center.y, 45, 37);
            if (_presenter.Reason(BattleAction.Transform) == null)
            {
                SpriteButton("control/bs", beast, tooltip: "炎兽变身", disabledImage: false);
            }
            else
                RegisterControl("transform", beast, SpriteTexture("control/unbs"));
        }

        private void DrawAbilities()
        {
            bool item = _presenter.Page == BattleHudPage.Items;
            bool magic = _presenter.Page == BattleHudPage.Magic;
            string folder = item ? "ItemList" : magic ? "MagicList" : "SkillList";
            Rect panel = BattleHudText.AbilityPanelBounds(_presenter.Page, _layout.IsPal3A);
            Sprite("control/" + folder + "/" + (item ? "ItemPanel" : magic ? "MagicPanel" : "SkillPanel"), panel);
            string[] tabs = item ? new[] { "recover", "attack", "treasure" } : magic ?
                new[] { "water", "fire", "thunder", "wind", "earth", "double" } : Array.Empty<string>();
            string[] labels = item ? new[] { "恢复", "攻击", "仙宝" } : new[] { "水", "火", "雷", "风", "土", "双系" };
            for (int index = 0; index < tabs.Length; index++)
                SpriteButton("control/" + folder + "/" + tabs[index], new Rect(panel.x + 40 + index * 32, panel.y + 27, 32, 32),
                    tooltip: labels[index], selected: _presenter.Category == index);
            BattleHudAbility[] abilities = _presenter.Abilities().ToArray();
            Rect list = BattleHudText.ListBounds(_presenter.Page, _layout.IsPal3A);
            list.position += panel.position;
            float height = list.height;
            const float rowHeight = 19;
            string focus = _renderFocusedId;
            int focusedIndex = Array.FindIndex(abilities, row => "ability:" + row.Definition.Id == focus);
            if (_scrollFocus != focus && focusedIndex >= 0)
            {
                float rowTop = focusedIndex * rowHeight;
                _listScroll.y = Math.Clamp(_listScroll.y, Math.Max(0, rowTop + rowHeight - list.height), rowTop);
                _scrollFocus = focus;
            }
            RegisterTouchList(list, Math.Max(height, abilities.Length * rowHeight));
            _listScroll = GUI.BeginScrollView(list, _listScroll, new Rect(0, 0, list.width - 15, Math.Max(height, abilities.Length * rowHeight)), false, false);
            BattleHudAbility hovered = null, focused = null;
            GUIStyle availableStyle = BattleHudCanvas.LabelStyle(13, new Color(5 / 255f, 5 / 255f, 0));
            GUIStyle deniedStyle = BattleHudCanvas.LabelStyle(13, new Color(.35f, .37f, .35f));
            try
            {
                for (int index = 0; index < abilities.Length; index++)
                {
                    BattleHudAbility row = abilities[index];
                    Rect rect = new(0, index * rowHeight, list.width - 16, rowHeight);
                    string controlId = "ability:" + row.Definition.Id;
                    Rect hit = new(list.x + rect.x, list.y + rect.y - _listScroll.y, rect.width, rect.height);
                    if (hit.yMax > list.y && hit.y < list.yMax)
                    {
                        float low = Math.Max(hit.y, list.y);
                        float high = Math.Min(hit.yMax, list.yMax);
                        RegisterControl(controlId, new Rect(hit.x, low, hit.width, high - low));
                    }
                    bool hover = !_navigationInput && list.Contains(_pointer) && rect.Contains(_pointer - list.position + _listScroll);
                    if (hover || focus == controlId)
                    {
                        EditorGUI.DrawRect(rect, new Color(1, .8f, .3f, .22f));
                        if (hover)
                            hovered = row;
                        if (focus == controlId)
                            focused = row;
                    }
                    GUIStyle style = row.Denied == null ? availableStyle : deniedStyle;
                    float nameWidth = magic ? 96 : Math.Min(150, rect.width - 43);
                    GUI.Label(new Rect(rect.x, rect.y, nameWidth, rect.height), row.Definition.Name, style);
                    if (magic)
                    {
                        int cost = row.Definition.Costs.Where(value => value.Resource == 2).Sum(value => value.Value);
                        GUI.Label(new Rect(rect.x + 96, rect.y, 37, rect.height), cost.ToString(), style);
                        GUI.Label(new Rect(rect.x + 133, rect.y, 50, rect.height), $"{row.Level}/{row.Uses:00}", style);
                    }
                    else
                    {
                        int cost = row.Definition.Costs.Where(value => value.Resource == 1).Sum(value => value.Value);
                        GUI.Label(new Rect(rect.x + nameWidth, rect.y, rect.width - nameWidth, rect.height), item ? row.Count.ToString() : cost.ToString(), style);
                    }
                    if (row.Denied != null)
                    {
                        string rowTooltip = BattleHudDenialText.ForAbility(_battle, _presenter, row.Definition, detailed: true);
                        GUI.Label(rect, new GUIContent("", rowTooltip), GUIStyle.none);
                        // Match the clipped, scaled geometry used by row hover.
                        if (hover)
                            GUI.tooltip = rowTooltip;
                    }
                }

            }
            finally
            {
                GUI.EndScrollView();
            }
            BattleHudAbility described = hovered ?? focused;
            string description = abilities.Length == 0 ? "此分类没有可用条目" : described != null ? Description(described.Definition) : "";
            string tooltip = described != null ? BattleHudDenialText.ForAbility(_battle, _presenter, described.Definition, detailed: true) ?? description : description;
            DrawDescription(description, tooltip, panel, item);
        }

        private string _descriptionText;
        private string _descriptionTooltip;
        private Rect _descriptionRect;

        private void DrawDescription(string text, string tooltip, Rect panel, bool item)
        {
            Rect area = BattleHudText.DescriptionBounds(_presenter.Page, _layout.IsPal3A);
            area.position += panel.position;
            const int size = BattleHudText.DescriptionFontSize;
            GUIStyle style = BattleHudCanvas.LabelStyle(size,
                item ? new Color(5 / 255f, 5 / 255f, 0) : new Color(0, 0, 0, 0x90 / 255f), tight: true);
            _descriptionText = text;
            _descriptionTooltip = tooltip;
            _descriptionRect = new Rect(_pageOffset + area.position * _pageScale, area.size * _pageScale);
            GUI.BeginGroup(area);
            try
            {
                foreach (BattleHudTextGlyph glyph in BattleHudText.Layout(text, area.width, area.height, size))
                    GUI.Label(new Rect(glyph.Column * size / 2f, glyph.Row * size, size, size), glyph.Text, style);
            }
            finally
            {
                GUI.EndGroup();
            }
            GUI.Label(area, new GUIContent("", tooltip), GUIStyle.none);
        }

        private string Description(BattleAbilityDefinition ability)
        {
            string text = ability.IsItem ? _data.Database.GameItemInfos.TryGetValue(ability.Id, out var item) ? item.Description : "" :
                _data.Database.SkillInfos.TryGetValue(ability.Id, out var skill) ? skill.Description : "";
            string denied = BattleHudDenialText.ForAbility(_battle, _presenter, ability);
            return denied ?? BattleHudText.Description(text);
        }

    }
}
