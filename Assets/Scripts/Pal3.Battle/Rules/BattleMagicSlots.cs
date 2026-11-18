// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Battle
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public readonly struct BattleMagicSlot
    {
        public int Group
        {
            get;
        }

        public int Slot
        {
            get;
        }

        public BattleAbilityDefinition Definition
        {
            get;
        }

        public int SkillId => Definition.Id;

        public BattleMagicSlot(int group, int slot, BattleAbilityDefinition definition)
        {
            Group = group;
            Slot = slot;
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
        }
    }

    // Native magic groups are water, fire, thunder, wind, earth, composite.
    // Explicit save/authoring slots override LoadUseableMagic's group/ID ordering.
    // Group 6 retains old synthetic definitions without an element attribute.
    public static class BattleMagicSlots
    {
        public static int Group(int elementMask) => elementMask switch
        {
            1 => 0,
            2 => 1,
            8 => 2,
            4 => 3,
            16 => 4,
            > 0 when (elementMask & (elementMask - 1)) != 0 => 5,
            _ => 6
        };

        public static bool IsCatalogueMagic(BattleAbilityDefinition ability) => ability.SkillTypeId is >= 1 and <= 3 ||
            ability.SkillTypeId == -1 && ability.IsMagic;

        public static IReadOnlyList<BattleMagicSlot> Capture(string variant, BattleDefinitions definitions, int identity,
            IReadOnlyList<BattleSkill> skills) => CaptureSlots(variant, definitions, identity,
            skills?.Select(skill => skill == null ? throw new ArgumentException("Missing magic skill.") :
                (skill.Id, skill.NativeMagicGroup, skill.NativeMagicSlot)));

        public static IReadOnlyList<BattleMagicSlot> Capture(string variant, BattleDefinitions definitions, int identity,
            IReadOnlyList<BattleSkillSnapshot> skills) => CaptureSlots(variant, definitions, identity,
            skills?.Select(skill => skill == null ? throw new ArgumentException("Missing magic skill.") :
                (skill.Id, skill.NativeMagicGroup, skill.NativeMagicSlot)));

        private static IReadOnlyList<BattleMagicSlot> CaptureSlots(string variant, BattleDefinitions definitions, int identity,
            IEnumerable<(int Id, int Group, int Slot)> skills)
        {
            if (variant is not ("PAL3" or "PAL3A") || definitions == null || skills == null)
                throw new ArgumentException("Magic slots require a variant, captured definitions and actor skills.");
            if (definitions.Variant != variant)
                throw new ArgumentException("Invalid native magic group and slot.");
            var explicitSlots = new Dictionary<int, (int Group, int Slot)>();
            foreach (var skill in skills)
            {
                bool specified = skill.Group != -1 || skill.Slot != -1;
                if (specified && (skill.Group < 0 || skill.Group >= 6 || skill.Slot < 0 || skill.Slot >= (variant == "PAL3A" ? 9 : 8)))
                    throw new ArgumentException("Invalid native magic group and slot.");
                if (specified)
                    explicitSlots.Add(skill.Id, (skill.Group, skill.Slot));
            }
            var occupied = new HashSet<(int Group, int Slot)>();
            var result = new List<BattleMagicSlot>();
            BattleAbilityDefinition[] legal = definitions.SkillsInOriginalOrder.Where(ability => IsCatalogueMagic(ability) &&
                ability.ApplicableIdentities.Contains(identity)).ToArray();
            foreach (BattleAbilityDefinition ability in legal.Where(ability => explicitSlots.ContainsKey(ability.Id)))
            {
                var position = explicitSlots[ability.Id];
                if (position.Group < 0 || position.Group >= 6 || position.Slot < 0 || position.Slot >= (variant == "PAL3A" ? 9 : 8) || !occupied.Add(position))
                    throw new ArgumentException("Conflicting native magic slots.");
                result.Add(new BattleMagicSlot(position.Group, position.Slot, ability));
            }
            foreach (BattleAbilityDefinition ability in legal.Where(ability => !explicitSlots.ContainsKey(ability.Id)).OrderBy(ability => Group(ability.ElementMask)).ThenBy(ability => ability.Id))
            {
                int group = Group(ability.ElementMask);
                int slot = 0;
                while (occupied.Contains((group, slot)))
                    slot++;
                occupied.Add((group, slot));
                result.Add(new BattleMagicSlot(group, slot, ability));
            }
            return Array.AsReadOnly(result.OrderBy(slot => slot.Group).ThenBy(slot => slot.Slot).ToArray());
        }
    }
}
