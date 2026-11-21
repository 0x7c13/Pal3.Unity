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
    using UnityEditor;
    using UnityEngine;

    public sealed partial class BattleEditorWindow
    {
        private BattleAction _battleAction;
        private int _battleAbility;
        private string _commandActor;
        private string _protectionController, _protectionSelection;

        private void StartBattle()
        {
            _session.StartBattle(Scenario, _autoAllies, _autoEnemies);
            _previewJson = BattleScenarioJson.Serialize(Scenario);
            _commandActor = null;
            Log("战斗已创建；技能、物品、装备、状态与局内成长使用已校验的原版规则。未覆盖语义会明确拒绝，完整边界见诊断。使用顶部主操作播放，或在诊断中单步。");
        }

        private void DrawBattleBody()
        {
            BattleSession combat = _session.Battle;
            var participants = combat.Actors.ToDictionary(actor => actor.Id, actor => combat.CaptureParticipant(actor.Id));
            string Label(string id) => id != null && participants.TryGetValue(id, out BattleParticipant actor) ? ActorLabel(actor) : id;
            GUILayout.Label($"战场 {combat.ActiveScene} · Boss 回合 {combat.BossRounds}", EditorStyles.wordWrappedMiniLabel);
            if (combat.Definitions.Variant == "PAL3A" && combat.FiveSpiritElement != 0)
                GUILayout.Label($"五灵法阵：{(Core.Contract.Enums.ElementType)combat.FiveSpiritElement} · {(combat.FiveSpiritForce == 1 ? "我方" : "敌方")}", EditorStyles.wordWrappedMiniLabel);
            if (combat.Definitions.Variant == "PAL3A")
                GUILayout.Label("五灵轮（水／火／风／雷／土）：" + string.Join("／", combat.Pal3AState.SpiritWheel.Charges) +
                    $" · 炎兽 {combat.Pal3AState.SpiritWheel.NangongRounds} 回合 · 累计精请求 {combat.Pal3AState.HpLoss} · 神请求 {combat.Pal3AState.MpLoss}", EditorStyles.wordWrappedMiniLabel);
            GUILayout.Label($"本局金钱 {combat.Money} · 物品 {string.Join("、", combat.Inventory.Select(item => $"{AbilityName(combat, item.Id, true)} ×{item.Count}"))}", EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.HelpBox("原版 AI 使用同一套技能、物品、资源和状态规则；可逐角色配置，或手动下令后重放记录。", MessageType.Info);
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(combat.Status != BattleSessionStatus.Running))
                {
                    foreach (BattleSide side in Enum.GetValues(typeof(BattleSide)))
                    {
                        bool before = combat.IsAutomatic(side);
                        bool automatic;
                        using (new EditorGUI.DisabledScope(combat.ActiveFormationController != null))
                            automatic = GUILayout.Toggle(before, (side == BattleSide.Ally ? "我方" : "敌方") + "自动");
                        if (before != automatic)
                            combat.SetAutomatic(side, automatic);
                    }
                }
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(combat.Status != BattleSessionStatus.Running))
                {
                    if (DrawerButton("下一命中"))
                        Try(() => _session.AdvanceTo(BattleEventKind.Hit));
                    if (DrawerButton("下一行动结束"))
                        Try(() => _session.AdvanceTo(BattleEventKind.ActionEnded));
                    if (DrawerButton("中止战斗"))
                    {
                        combat.Abort();
                        _session.SyncBattle();
                    }
                }
                if (DrawerButton("重放记录"))
                    Try(_session.BeginReplay, pauseOnError: false);
            }
            if (combat.Result != null)
            {
                if (_session.ResultPresentation is { } presentation)
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        using (new EditorGUI.DisabledScope(!presentation.CanSkip))
                            if (DrawerButton("跳过当前提示"))
                                _session.SkipResultStep();
                        if (DrawerButton("重播结果演出"))
                            Try(_session.ReplayResults, pauseOnError: false);
                    }
                }
                DrawBattleResult(combat.Result, Label);
            }
            else if (combat.ActiveFormationController is { } controller)
                DrawFormationControl(combat, controller, Label);
            else if (combat.WaitingActor is { } waiting)
                DrawBattleCommand(combat, waiting, Label);
            if (combat.FleeResult is { } fleeResult)
                EditorGUILayout.HelpBox(fleeResult.Success ? "逃跑成功" : "逃跑失败", MessageType.Info);
            foreach (BattleActorSnapshot actor in combat.Actors)
            {
                Rect row = EditorGUILayout.GetControlRect(false, 20);
                EditorGUI.ProgressBar(row, actor.Gauge / (float)BattleSession.FullGauge,
                    $"{Label(actor.Id)} · 精 {actor.Hp}/{actor.MaxHp} · 气 {actor.Sp}/{actor.MaxSp} · 神 {actor.Mp}/{actor.MaxMp} · {actor.Pose}{(actor.Ready ? " · 就绪" : "")}");
                if (Event.current.type == EventType.MouseDown && row.Contains(Event.current.mousePosition))
                {
                    _selected = actor.Id;
                    Event.current.Use();
                }
                if (actor.Pose is BattlePose.Casting or BattlePose.Fleeing)
                {
                    Rect cast = EditorGUILayout.GetControlRect(false, 16);
                    string action = actor.Pose == BattlePose.Fleeing ? "逃跑等待" : AbilityName(combat, actor.AbilityId, false);
                    EditorGUI.ProgressBar(cast, actor.CastGauge / (float)BattleSession.FullCastGauge, $"第二槽 {actor.CastGauge}/{BattleSession.FullCastGauge} · {action}");
                }
            }
            if (combat.Actors.FirstOrDefault(actor => actor.Id == _selected) is { } selected)
            {
                GUILayout.Label($"{Label(selected.Id)} · 等级 {selected.Level} · 经验 {selected.Experience} · 回合 {selected.Rounds} · 好感 {selected.Love}", EditorStyles.boldLabel);
                GUILayout.Label("当前属性：" + string.Join(" · ", selected.Attributes.Select((value, index) => $"{AttributeLabels[index]} {value}")), EditorStyles.wordWrappedMiniLabel);
                GUILayout.Label("状态：" + (selected.States.Count == 0 ? "无" : string.Join("、", selected.States.Select(state => $"{state.State}（{(state.Permanent ? "永久" : $"{state.RemainingRounds}回合")}{(state.LatentRounds > 0 ? $"，潜伏{state.LatentRounds}" : "")}）"))), EditorStyles.wordWrappedMiniLabel);
                if (selected.MagicSword is { } sword)
                    GUILayout.Label($"魔剑：{AbilityName(combat, sword.SkillId, false)} · 能量 {sword.Energy:F2}/200 · 净化 {sword.PurificationCount} 次", EditorStyles.wordWrappedMiniLabel);
                GUILayout.Label("技能熟练：" + string.Join("、", selected.Skills.Select(skill => $"{AbilityName(combat, skill.Id, false)} Lv{skill.Level} · {skill.Uses}次{(skill.Mastered ? " · 掌握" : "")}")), EditorStyles.wordWrappedMiniLabel);
                DrawAiControls(combat, selected);
                if (selected.ActionId != 0 && DrawerButton("取消所选角色行动（调试，保留已消耗资源）"))
                    Try(() =>
                    {
                        combat.CancelAction(selected.Id);
                        _session.SyncBattle();
                    });
            }
        }

        private void DrawFormationControl(BattleSession combat, string controller, Func<string, string> label)
        {
            GUILayout.Label("阵法 · " + label(controller), EditorStyles.boldLabel);
            GUILayout.Label("可连续调整阵位；确认时只清操作者的第一槽。最终还原原阵型不耗条。", EditorStyles.wordWrappedMiniLabel);
            foreach (BattleActorSnapshot actor in combat.Actors.Where(actor => actor.Side == BattleSide.Ally))
            {
                string denied = combat.CanSwapFormation(actor.Id, actor.Slot);
                using (new EditorGUI.DisabledScope(denied != null))
                {
                    BattleSlot next = (BattleSlot)EditorGUILayout.EnumPopup(label(actor.Id), actor.Slot);
                    if (next != actor.Slot)
                        Try(() =>
                        {
                            string reason = combat.CanSwapFormation(actor.Id, next);
                            if (reason != null)
                                throw new InvalidOperationException(reason);
                            combat.SwapFormation(actor.Id, next);
                            _session.SyncBattle();
                        });
                }
                if (denied != null)
                    GUILayout.Label(denied, EditorStyles.wordWrappedMiniLabel);
            }
            if (DrawerButton("确认阵型"))
                Try(() =>
                {
                    combat.EndFormation(controller);
                    _session.SyncBattle();
                });
        }

        private bool DrawProtectionControl(BattleSession combat, string controller, Func<string, string> label)
        {
            if (combat.CanSetProtection(controller, null) != null)
                return false;
            string[] targets = new[] { (string)null }.Concat(combat.Actors.Where(actor => actor.Side == BattleSide.Ally &&
                combat.CanSetProtection(controller, actor.Id) == null).Select(actor => actor.Id)).ToArray();
            if (_protectionController != controller)
            {
                _protectionController = controller;
                _protectionSelection = combat.ProtectionTarget;
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                int selected = Math.Max(0, Array.IndexOf(targets, _protectionSelection));
                selected = EditorGUILayout.Popup("援护对象", selected, targets.Select(id => id == null ? "不援护" : label(id)).ToArray(), GUILayout.Height(BattleEditorTheme.ControlHeight));
                _protectionSelection = targets[selected];
                using var disabled = new EditorGUI.DisabledScope(_protectionSelection == combat.ProtectionTarget);
                if (DrawerButton("确认援护", GUILayout.Width(90)))
                {
                    bool changed = combat.SetProtection(controller, _protectionSelection);
                    _session.SyncBattle();
                    return changed;
                }
            }
            return false;
        }

        private void DrawBattleCommand(BattleSession combat, string waiting, Func<string, string> label)
        {
            BattleActorSnapshot actor = combat.Actors.First(candidate => candidate.Id == waiting);
            if (_commandActor != waiting)
            {
                _commandActor = waiting;
                _battleAction = BattleAction.Attack;
                _battleAbility = 0;
            }
            GUILayout.Label("等待 " + label(waiting), EditorStyles.boldLabel);
            if (actor.Side == BattleSide.Ally)
            {
                string lineupReason = combat.CanBeginFormation(waiting);
                using (new EditorGUI.DisabledScope(lineupReason != null))
                    if (DrawerButton("阵法换位"))
                        Try(() => combat.BeginFormation(waiting));
                if (lineupReason != null)
                    GUILayout.Label(lineupReason, EditorStyles.wordWrappedMiniLabel);
                if (DrawProtectionControl(combat, waiting, label))
                    return;
            }
            _battleAction = (BattleAction)DrawerTabs("tabs:battle-action", (int)_battleAction, combat.Definitions.Variant == "PAL3A" ? new[] { "普攻", "防御", "技能", "物品", "逃跑", "炎兽变身" } : new[] { "普攻", "防御", "技能", "物品", "逃跑" });
            BattleAbilityDefinition ability = null;
            if (_battleAction is BattleAction.Skill or BattleAction.Item)
            {
                bool item = _battleAction == BattleAction.Item;
                IReadOnlyDictionary<int, BattleAbilityDefinition> definitions = item ? combat.Definitions.Items : combat.Definitions.Skills;
                int[] ids = (item ? combat.Inventory.Where(entry => entry.Count > 0).Select(entry => entry.Id) : actor.Skills.Select(skill => skill.Id)
                    .Concat(actor.MagicSword is { SkillId: > 0 } sword ? new[] { sword.SkillId } : Array.Empty<int>())).Distinct().ToArray();
                int selected = Math.Max(0, Array.IndexOf(ids, _battleAbility));
                selected = EditorGUILayout.Popup(item ? "本局物品" : "已学技能", selected, ids.Select(id =>
                {
                    if (item)
                        return $"{AbilityName(combat, id, true)} ×{combat.Inventory.First(entry => entry.Id == id).Count}";
                    if (actor.MagicSword?.SkillId == id)
                        return $"{AbilityName(combat, id, false)} · 魔剑 {actor.MagicSword.Energy:F2}/200";
                    BattleSkillSnapshot skill = actor.Skills.First(entry => entry.Id == id);
                    return $"{AbilityName(combat, id, false)} · Lv{skill.Level} · {skill.Uses}次{(skill.Mastered ? " · 掌握" : "")}";
                }).ToArray());
                _battleAbility = ids.Length > 0 ? ids[selected] : 0;
                if (definitions.TryGetValue(_battleAbility, out ability))
                    GUILayout.Label($"范围：{ability.Range}{(ability.Revives ? " · 可选择死亡角色" : "")}", EditorStyles.wordWrappedMiniLabel);
                else
                    GUILayout.Label(item ? "本局没有可用物品。" : "此角色未配置技能。", EditorStyles.wordWrappedMiniLabel);
            }
            int abilityId = _battleAction is BattleAction.Skill or BattleAction.Item ? _battleAbility : 0;
            var query = new BattleCommand(waiting, _battleAction, abilityId: abilityId);
            BattleActorSnapshot[] targets = combat.GetTargetCandidates(query).ToArray();
            if (targets.Length > 0)
            {
                int targetIndex = Math.Max(0, Array.FindIndex(targets, target => target.Id == _target));
                targetIndex = EditorGUILayout.Popup("合法目标／范围中心", targetIndex, targets.Select(target => label(target.Id) + (target.Alive ? "" : " · 死亡")).ToArray());
                _target = targets[targetIndex].Id;
            }
            else
                _target = null;
            var command = new BattleCommand(waiting, _battleAction, _target, abilityId);
            string reason = combat.CanSubmit(command);
            BattleCostEstimate[] costs = combat.GetCostEstimate(command).ToArray();
            if (costs.Length > 0)
                GUILayout.Label("消耗预估：" + string.Join(" · ", costs.Select(cost => $"{ResourceName(cost.Resource)} 需求{cost.Required}／实付{cost.Payable}／持有{cost.Available}")), EditorStyles.wordWrappedMiniLabel);
            if (_battleAction == BattleAction.Item && ability != null)
                GUILayout.Label($"消耗预估：物品 1 件／持有 {combat.Inventory.FirstOrDefault(item => item.Id == ability.Id)?.Count ?? 0} 件", EditorStyles.wordWrappedMiniLabel);
            if (reason != null)
                EditorGUILayout.HelpBox(reason, MessageType.Warning);
            using (new EditorGUI.DisabledScope(reason != null))
                if (DrawerButton("执行指令"))
                    Try(() =>
                    {
                        combat.Submit(command);
                        _session.SyncBattle();
                    });
            GUILayout.Label("输入选择与校验不会扣资源或消耗随机数；配置草稿的修改在下次开局生效。", EditorStyles.wordWrappedMiniLabel);
        }

        private static string AbilityName(BattleSession combat, int id, bool item)
        {
            var definitions = item ? combat.Definitions.Items : combat.Definitions.Skills;
            return definitions.TryGetValue(id, out BattleAbilityDefinition ability) ? $"{ability.Name} ({id})" : id.ToString();
        }

        private static string ResourceName(int resource) => resource switch
        {
            0 => "精",
            1 => "气",
            2 => "神",
            10 => "金钱",
            16 => "蛊",
            _ => $"资源{resource}"
        };

        private static void DrawBattleResult(BattleResult result, Func<string, string> label)
        {
            GUILayout.Label($"{result.Status} · 胜方：{result.Winner?.ToString() ?? "无"} · {(result.Escaped ? "已逃跑 · " : "")}{result.Reason}", EditorStyles.boldLabel);
            GUILayout.Label($"本局金钱 {result.MoneyBefore} → {result.MoneyAfter}", EditorStyles.wordWrappedMiniLabel);
            if (result.Pal3A != null)
                GUILayout.Label("五灵轮：" + string.Join("／", result.Pal3A.SpiritWheel.Charges) + $" · 累计精请求 {result.Pal3A.HpLoss} · 神请求 {result.Pal3A.MpLoss}", EditorStyles.wordWrappedMiniLabel);
            foreach (BattleActorReward reward in result.Rewards ?? Array.Empty<BattleActorReward>())
            {
                GUILayout.Label($"{label(reward.Actor)}：经验 +{reward.ExperienceGained}（{reward.ExperienceBefore} → {reward.ExperienceAfter}），等级 {reward.LevelBefore} → {reward.LevelAfter}", EditorStyles.wordWrappedMiniLabel);
                if (reward.AttributesBefore != null && reward.AttributesAfter != null)
                    GUILayout.Label("属性成长：" + string.Join(" · ", reward.AttributesBefore.Select((before, index) => (before, after: reward.AttributesAfter[index], index))
                        .Where(value => value.before != value.after).Select(value => $"{AttributeLabels[value.index]} {value.before} → {value.after}")), EditorStyles.wordWrappedMiniLabel);
            }
            GUILayout.Label("掉落：" + string.Join("、", (result.Loot ?? Array.Empty<BattleItemSnapshot>()).Select(item => $"{item.Id} ×{item.Count}")), EditorStyles.wordWrappedMiniLabel);
            GUILayout.Label("结算只保留在本次独立战斗报告中。", EditorStyles.wordWrappedMiniLabel);
        }
    }
}
