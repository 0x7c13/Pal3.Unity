// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.BattleEditor
{
    using System;
    using System.Collections.Generic;
    using Battle;
    using Battle.Presentation;

    // Domain reasons remain diagnostics. Only known messages and typed cost
    // estimates cross this boundary into the player's description panel.
    public static class BattleHudDenialText
    {
        public static string ForAbility(BattleSession session, BattleHudPresenter presenter, BattleAbilityDefinition ability, bool detailed = false)
        {
            if (presenter == null || ability == null)
                return detailed ? "当前无法使用。" : "当前无法使用";
            BattleAction action = ability.IsItem ? BattleAction.Item : BattleAction.Skill;
            string reason = presenter.Reason(action, ability.Id);
            IReadOnlyList<BattleCostEstimate> costs = IsResourceDenial(reason) && session != null && presenter.Controller != null
                ? session.GetCostEstimate(new BattleCommand(presenter.Controller, action, abilityId: ability.Id)) : null;
            return Format(reason, costs, detailed);
        }

        public static string Format(string reason, IEnumerable<BattleCostEstimate> costs = null, bool detailed = false)
        {
            if (reason == null)
                return null;
            if (IsResourceDenial(reason))
            {
                // Keep the same first failing cost and pre-savings requirement
                // as CanSubmit; parsing the diagnostic would lose this context.
                if (costs != null)
                    foreach (BattleCostEstimate cost in costs)
                        if (cost.Required > cost.Available)
                        {
                            string resource = ResourceName(cost.Resource);
                            if (resource == null)
                                return detailed ? "施放所需资源不足。" : "所需资源不足";
                            if (detailed)
                                return $"{resource}不足：需要 {cost.Required}，当前 {cost.Available}。";
                            string brief = $"{resource}不足：需{cost.Required}，现{cost.Available}";
                            return FitsBriefLine(brief) ? brief : resource + "不足";
                        }
                return detailed ? "施放所需资源不足。" : "所需资源不足";
            }
            string text = reason switch
            {
                "先启用 AI，再设置此项。" => "先启用 AI，再设置此项。",
                "等待角色就绪" or "Actor is not ready." or "The protection controller is not ready." => "等待角色就绪。",
                "An earlier actor is waiting for input." => "请先为前一位角色下达指令。",
                "The battle has ended." => "战斗已经结束。",
                "The original combat movie is playing." => "请等待战斗动画结束。",
                "The encounter outcome is resolving." => "正在结算战斗结果。",
                "Finish formation control before submitting an action." => "请先完成阵位调整。",
                "定／眠期间不能行动。" => "定／眠期间不能行动。",
                "乱／狂期间由原版异常状态 AI 接管。" => "乱／狂期间不能手动下达指令。",
                "禁期间不能使用物品。" => "禁期间不能使用物品。",
                "封期间不能施放仙术。" => "封期间不能施放仙术。",
                "There is no legal target." => "没有可选目标。",
                "Choose a legal target for this action." => "请选择可用目标。",
                "Choose a scene element before resolving allied rows or columns." => "请先设置战场五灵属性。",
                "This actor has not mastered the skill." => "尚未学会此技能。",
                "This skill belongs to another character." => "此角色不能使用该技能。",
                "The original skill proficiency must be 1–4." => "技能熟练等级无效，暂时无法使用。",
                "This item cannot be used in combat." => "此物品不能在战斗中使用。",
                "Original enemies cannot use inventory items." => "敌方不能使用背包物品。",
                "No item remains in this battle inventory." => "此物品已用完或已被其他指令占用。",
                "The magic sword energy must be greater than 199.9." => "魔剑能量不足。",
                "This actor has no captured magic sword with the selected weapon skill." => "请装备拥有此魔剑技的魔剑。",
                "Transformation is exclusive to PAL3A." => "此版本无法变身。",
                "The original form transition is still running." => "请等待变身结束。",
                "Only Nangong Huang's human form can request the fire beast transformation." => "只有人形的南宫煌可以炎兽变身。",
                "All five spirit wheel charges must reach three." => "五灵轮各系均需充能至少三次。",
                "朱仙变 requires Wang Pengxu's human form." => "只有人形的王蓬絮可以使用朱仙变。",
                "朱仙变 requires full SP before equipment savings." => "气满后才能使用朱仙变。",
                "摄灵法阵 requires Nangong Huang in native slot zero." => "只有南宫煌可以使用摄灵法阵。",
                "The original fire beast form permits only physical attack, defense, or escape." => "炎兽形态只能普通攻击、防御或逃跑。",
                "The original fairy beast form permits only magic, defense, or escape." => "朱仙形态只能施放仙术、防御或逃跑。",
                "Choose a PAL3A five-spirit wheel element." => "请选择五灵轮属性。",
                "Choose the current controller." or "Choose the current ready controller." or "Choose the current ready allied controller." => "请先选择当前就绪角色。",
                "The spirit wheel needs Nangong Huang as the current ready controller." => "请等待南宫煌就绪。",
                "This spirit wheel element needs nine charges." => "此系五灵轮需充能九次。",
                "A formation panel is already open." => "请先完成当前阵位调整。",
                "Open the formation panel first." => "请先打开阵位面板。",
                "Only an allied participant can change formation." => "只有我方角色可以调整阵位。",
                "Unknown formation slot." => "请选择可用阵位。",
                "The protection target is missing." => "请选择援护对象。",
                "Original protection requires the living player in native slot 0 at the center." => "援护者须存活并位于中央阵位。",
                "Unknown skill or item." => "此技能或物品暂时无法使用。",
                "This ability has an unsupported resource cost." => "此技能的消耗暂未支持。",
                "The original form animation catalog is missing." or
                "The original spirit wheel presentation is missing." or
                "Spirit capture needs the original screen-space wheel layout." or
                "This attribute effect needs its original dedicated battle handler." or
                "This ability contains an unverified target range or derived/passive state operation." or
                "The magic sword needs its original effect completion director." or
                "This ability requires its original effect completion director." or
                "The ability has no verified action timeline." or
                "This ability requires a dedicated repeated-effect timeline." => "此技能暂时无法使用。",
                "Random formation needs the original scene's formation positions." => "此战场暂时无法使用阵位变化。",
                "This treasure has no original usage counter in the selected variant." => "此仙宝在当前版本暂时无法使用。",
                _ => "当前无法使用。"
            };
            return detailed ? text : BriefReason(text);
        }

        private static string BriefReason(string text) => text switch
        {
            "先启用 AI，再设置此项。" => "请先启用AI",
            "请先为前一位角色下达指令。" => "等待前位角色指令",
            "乱／狂期间不能手动下达指令。" => "乱／狂期间自动行动",
            "技能熟练等级无效，暂时无法使用。" => "技能熟练等级无效",
            "此物品不能在战斗中使用。" => "战斗中不可使用",
            "此物品已用完或已被其他指令占用。" => "此物品无可用数量",
            "请装备拥有此魔剑技的魔剑。" => "未装备相应魔剑技",
            "只有人形的南宫煌可以炎兽变身。" => "仅人形南宫煌可变身",
            "五灵轮各系均需充能至少三次。" => "五灵轮各系需三次",
            "只有人形的王蓬絮可以使用朱仙变。" => "仅人形王蓬絮可使用",
            "只有南宫煌可以使用摄灵法阵。" => "仅南宫煌可使用",
            "炎兽形态只能普通攻击、防御或逃跑。" => "炎兽仅可攻、防、逃",
            "朱仙形态只能施放仙术、防御或逃跑。" => "朱仙仅可术、防、逃",
            "只有我方角色可以调整阵位。" => "仅我方可调整阵位",
            "援护者须存活并位于中央阵位。" => "援护者须存活且居中",
            "此技能或物品暂时无法使用。" => "当前暂不可使用",
            "此战场暂时无法使用阵位变化。" => "当前无法变化阵位",
            "此仙宝在当前版本暂时无法使用。" => "当前无法使用此仙宝",
            _ => text.TrimEnd('。')
        };

        // The narrowest native description band is 140 pixels at a 14-pixel
        // font. Large amounts keep their complete explanation in the tooltip.
        private static bool FitsBriefLine(string text)
        {
            int cells = 0;
            foreach (char character in text)
                cells += character < 0x80 ? 1 : 2;
            return cells <= 20;
        }

        private static bool IsResourceDenial(string reason) => reason?.StartsWith("Resource ", StringComparison.Ordinal) == true;

        private static string ResourceName(int resource) => resource switch
        {
            0 => "精",
            1 => "气",
            2 => "神",
            7 => "速度",
            8 => "运气",
            10 => "金钱",
            16 => "蛊",
            _ => null
        };
    }
}
