// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Battle
{
    using System;
    using System.Collections.Generic;

    public sealed partial class BattleAi
    {
        public int SelectMagic(int source, bool attack, bool group, int weight)
        {
            if (!Roles[source].Alive)
                return 0;
            var candidates = new List<BattleAiSkill>();
            int type = attack ? 2 : 3;
            int range = (attack ? 3 : 1) + (group ? 1 : 0);
            foreach (BattleAiSkill skill in Roles[source].Skills)
            {
                if (skill.Id != 0 && skill.Mastered && skill.IsMagic && skill.Type == type && skill.Range == range && _port.CanCast(source, skill.Id))
                    candidates.Add(skill);
            }
            int best = 0;
            int leastDistance = int.MaxValue;
            foreach (BattleAiSkill skill in candidates)
            {
                // The recovery comparator intentionally differs from the distance stored afterward.
                int distance = attack ? BattleBasicRules.SignedAbs(BattleBasicRules.SignedAbs(skill.Power) - BattleBasicRules.SignedAbs(weight)) : BattleBasicRules.SignedAbs(skill.Power - weight);
                if (distance < leastDistance)
                {
                    leastDistance = BattleBasicRules.SignedAbs(BattleBasicRules.SignedAbs(skill.Power) - BattleBasicRules.SignedAbs(weight));
                    best = skill.Id;
                }
            }
            return best;
        }

        // These are the original GetBestRole... support selectors. The caller supplies the
        // weight from the Cast boundary, including its native RNG draw for attack skills/items.
        public int SelectSupport(int source, BattleAiAttemptKind kind, bool group, int weight, int nativeState = 1)
        {
            if (!Roles[source].Alive)
                return 0;
            bool item = kind is BattleAiAttemptKind.RecoverItem or BattleAiAttemptKind.AttackItem or BattleAiAttemptKind.ReviveItem or BattleAiAttemptKind.CureItem;
            bool magic = kind is BattleAiAttemptKind.ReviveMagic or BattleAiAttemptKind.CureMagic;
            bool recover = kind is BattleAiAttemptKind.RecoverItem or BattleAiAttemptKind.RecoverSkill;
            bool attack = kind is BattleAiAttemptKind.AttackItem or BattleAiAttemptKind.AttackSkill;
            bool revive = kind is BattleAiAttemptKind.ReviveItem or BattleAiAttemptKind.ReviveSkill or BattleAiAttemptKind.ReviveMagic;
            bool cure = kind is BattleAiAttemptKind.CureItem or BattleAiAttemptKind.CureSkill or BattleAiAttemptKind.CureMagic;
            if (!recover && !attack && !revive && !cure)
                throw new ArgumentException("This attempt has no support selector.", nameof(kind));
            int best = 0;
            int minimum = int.MaxValue;
            foreach (BattleAiSkill skill in item ? Items : Roles[source].Skills)
            {
                if (skill.Id == 0 || item && skill.Count <= 0 || !item && (!skill.Mastered || skill.IsMagic != magic || skill.IsItem))
                    continue;
                // Ordinary item selectors check usability before reading the range/category.
                if (item && !revive && !cure && !_port.CanCast(source, skill.Id))
                    continue;
                bool rangeMatches = item ? group ? skill.Range is not (1 or 3) : skill.Range is not (2 or 4)
                    : skill.Range == (attack ? 3 : 1) + (group ? 1 : 0);
                if (!rangeMatches)
                    continue;
                if (!item && !magic && skill.Type != 0 || magic && source >= 5 && skill.Type == 0)
                    continue;
                bool category = recover ? item ? skill.Type == 1 && (skill.PowerMode == 2 || skill.PowerMode <= 1 && skill.Power > 0) : skill.Power > 0
                    : attack ? item ? skill.Type == 2 && skill.PowerMode <= 1 && skill.Power < 0 : skill.Power < 0
                    : Operation(skill, revive ? 25 : nativeState) == 2;
                if (!category)
                    continue;
                if ((!item || revive || cure) && !_port.CanCast(source, skill.Id))
                    continue;
                int power = item ? ItemPower(source, skill, recover) : skill.Power;
                if (item && recover && Operation(skill, 25) == 2)
                    power += 100000;
                int comparison = item ? BattleBasicRules.SignedAbs(BattleBasicRules.SignedAbs(power) - (cure ? 0 : weight))
                    : magic && revive ? BattleBasicRules.SignedAbs(BattleBasicRules.SignedAbs(power) - BattleBasicRules.SignedAbs(weight)) : BattleBasicRules.SignedAbs(power - (cure ? 0 : weight));
                if (comparison < minimum)
                {
                    minimum = BattleBasicRules.SignedAbs(BattleBasicRules.SignedAbs(power) - BattleBasicRules.SignedAbs(cure ? 0 : weight));
                    best = skill.Id;
                }
            }
            return best;
        }

        private static int Operation(BattleAiSkill skill, int nativeState)
            => nativeState == 0 ? skill.NullCureOperation : nativeState > 0 && nativeState <= skill.StateOperations.Length ? skill.StateOperations[nativeState - 1] : 0;

        private int ItemPower(int source, BattleAiSkill item, bool recover)
        {
            return item.PowerMode switch
            {
                0 => item.Power,
                1 => item.Power * Roles[source].MaxHp / 100,
                2 when recover => Roles[source].MaxHp - Roles[source].Hp,
                _ => 0
            };
        }

        public int ConvertSingleAttackMagic(int skillId)
        {
            if (skillId != (_pal3A ? 7714 : 7711))
                return skillId;
            int[] converted = { 207, 215, 232, 224, 240 };
            return converted[_random.Next() % converted.Length];
        }

        public bool IsSceneMagicForbidden(int source, int skillId, int elementMask)
        {
            int scene = _port.SceneFiveSpiritMode;
            return _pal3A && skillId != 0 && scene >= 1 && scene <= 5 && elementMask == 1 << (scene - 1)
                && _port.SceneFiveSpiritOwner != (Roles[source].Valid ? source < 5 ? 1 : 2 : 0);
        }

        public int SelectFiveSpiritMagic(int source, bool group, int weight = 0)
        {
            if (!Roles[source].Alive)
                return 0;
            int best = 0;
            int minimum = int.MaxValue;
            foreach (BattleAiSkill skill in Roles[source].Skills)
            {
                if (source < 5 && (!skill.Mastered || !skill.IsMagic))
                    continue;
                if (!group)
                {
                    if (skill.Id >= 264 && skill.Id <= 268)
                        return skill.Id;
                    continue;
                }
                int scene = _port.SceneFiveSpiritMode;
                if (skill.Id == 0 || skill.Type != 2 || skill.Range != 4 || scene < 1 || scene > 5 || skill.ElementMask != 1 << (scene - 1))
                    continue;
                if (!_port.CanCast(source, skill.Id))
                    continue;
                int distance = BattleBasicRules.SignedAbs(BattleBasicRules.SignedAbs(skill.Power) - BattleBasicRules.SignedAbs(weight));
                if (distance < minimum)
                {
                    minimum = distance;
                    best = skill.Id;
                }
            }
            return best;
        }

        public int SelectMiscMagic(int source, bool group)
        {
            if (!Roles[source].Alive)
                return 0;
            var candidates = new List<int>();
            foreach (BattleAiSkill skill in Roles[source].Skills)
            {
                if (skill.Id != 0 && skill.Mastered && skill.IsMagic && skill.Type == 1 && skill.Range == (group ? 2 : 1) && _port.CanCast(source, skill.Id))
                    candidates.Add(skill.Id);
            }
            return candidates.Count == 0 ? 0 : candidates[_random.Next() % candidates.Count];
        }

        public int QueryStatusMagic(int source, BattleAiStatusQuery query)
        {
            if (!Roles[source].Alive)
                return 0;
            var candidates = new List<int>();
            foreach (BattleAiSkill skill in Roles[source].Skills)
            {
                if (skill.Id == 0 || !MatchesStatusQuery(skill, query))
                    continue;
                if (_port.CanCast(source, skill.Id))
                    candidates.Add(skill.Id);
            }
            return candidates.Count == 0 ? 0 : candidates[_random.Next() % candidates.Count];
        }

        private static bool MatchesStatusQuery(BattleAiSkill skill, BattleAiStatusQuery query)
        {
            for (int state = 0; state < skill.StateOperations.Length; state++)
            {
                int operation = skill.StateOperations[state];
                bool matches = query switch
                {
                    BattleAiStatusQuery.Debuff => state is 6 or 8 or 10 or 12 or 13 or 14 or 15 or 16 or 17 or 18,
                    BattleAiStatusQuery.Buff => state is 5 or 7 or 9 or 11 or 19 or 20 or 21 or 22,
                    _ => state is >= 0 and <= 4 or 26
                };
                if (matches && (query == BattleAiStatusQuery.Poison ? operation != 0 : operation == 1))
                    return true;
            }
            return false;
        }

        public int MonsterSkillLevel(int source, int skillId)
        {
            int count = Math.Min(4, Roles[source].Skills.Count);
            for (int slot = 0; slot < count; slot++)
            {
                if (Roles[source].Skills[slot].Id == skillId)
                    return slot == 3 ? 4 : Roles[source].Skills[slot].Level;
            }
            return skillId is 207 or 215 or 224 or 232 or 240 ? 1 : 0;
        }

        public bool PlayerNormal(int source, bool playerCommand)
        {
            int target = SelectPlayerNormalTarget(source);
            if (target < 0)
                return false;
            Roles[source].Profile.Attack = true;
            bool accepted;
            try
            {
                accepted = Try(BattleAiAttemptKind.Normal, source, target);
            }
            finally
            {
                // Native F clears this bit even when it was previously enabled.
                Roles[source].Profile.Attack = false;
            }
            if (playerCommand)
            {
                _port.RecordPlayerNormal(source, target);
                NewRole[source] = false;
            }
            return accepted;
        }

        public int SelectPlayerNormalTarget(int source)
        {
            int best = -1;
            int count = 0;
            float minimum = 1.1f;
            for (int target = 5; target < 11; target++)
            {
                BattleAiRole actor = Roles[target];
                if (!PlayerNormalCandidate(source, target))
                    continue;
                count++;
                double ratio = (double)(float)actor.Hp / actor.MaxHp;
                if (_pal3A)
                    ratio = (float)ratio;
                if (ratio < minimum)
                {
                    minimum = (float)ratio;
                    best = target;
                }
            }
            if (count == 0 || best >= 0)
                return best;
            for (int target = 5; target < 11; target++)
            {
                if (PlayerNormalCandidate(source, target) && _random.Probability(100 / count))
                    return target;
            }
            return -1;
        }

        private bool PlayerNormalCandidate(int source, int target)
        {
            BattleAiRole actor = Roles[target];
            return actor.Valid && actor.Alive && (!_pal3A || !actor.Fled && !actor.Fleeing && (target != 6 || _port.CanAttackBack(source, target)));
        }
    }
}
