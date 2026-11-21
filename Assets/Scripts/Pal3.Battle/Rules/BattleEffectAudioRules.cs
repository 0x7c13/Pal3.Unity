// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Battle
{
    using System.Collections.Generic;
    using System.Linq;

    // SetSpecState, ChangeSpecState and ChangeRoleValue share these cues in both games.
    public static class BattleEffectAudioRules
    {
        public static string Recovery(bool hp, bool mp) => hp ? mp ? "wd003" : "wd001" : mp ? "wd002" : null;

        public static string State(BattleState state, bool remove)
        {
            if (remove)
                return state switch
                {
                    >= BattleState.PoisonWind and <= BattleState.PoisonEarth => "wd006",
                    BattleState.Reflection or BattleState.Evade or BattleState.Barrier or BattleState.Invisible => "wd020",
                    _ => null
                };
            return state switch
            {
                >= BattleState.PoisonWind and <= BattleState.PoisonEarth => "wd005",
                BattleState.AttackIncrease or BattleState.DefenseIncrease or BattleState.LuckIncrease or BattleState.SpeedIncrease => "wd021",
                BattleState.AttackDecrease or BattleState.DefenseDecrease or BattleState.LuckDecrease or BattleState.SpeedDecrease => "wd022",
                BattleState.Paralysis => "wd007",
                BattleState.Seal => "wd008",
                BattleState.Forbidden => "wd009",
                BattleState.Sleep => "wd010",
                BattleState.Madness => "wd011",
                BattleState.Chaos => "wd012",
                BattleState.Evade => "wd013",
                BattleState.Barrier => "wd014",
                BattleState.Invisible => "wd015",
                BattleState.Reflection => "wd016",
                _ => null
            };
        }

        public static IEnumerable<string> ClearedAilments(IEnumerable<BattleStatusEffect> effects)
        {
            var removed = new HashSet<BattleState>(effects.Where(effect => effect.Remove).Select(effect => effect.State));
            BattleState[] ailments = { BattleState.Paralysis, BattleState.Seal, BattleState.Forbidden, BattleState.Sleep, BattleState.Madness, BattleState.Chaos };
            if (ailments.All(removed.Contains))
                yield return "wd019";
            else
                foreach (BattleState state in ailments)
                    if (removed.Contains(state))
                        yield return state is BattleState.Paralysis or BattleState.Seal or BattleState.Forbidden ? "wd017" : "wd018";
        }
    }
}
