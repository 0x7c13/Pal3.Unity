// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Battle
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;

    // A session captures definitions, not a GDB cache, Unity object or mutable editor service.
    public sealed class BattleDefinitions
    {
        public string Variant
        {
            get;
        }

        public string Fingerprint
        {
            get;
        }

        public IReadOnlyDictionary<int, BattleAbilityDefinition> Skills
        {
            get;
        }

        public IReadOnlyDictionary<int, BattleAbilityDefinition> Items
        {
            get;
        }

        public IReadOnlyDictionary<string, BattleActorDefinition> Actors
        {
            get;
        }

        // Stream order is observable: the original picks the first eligible combination.
        public IReadOnlyList<BattleComboDefinition> Combos
        {
            get;
        }

        public IReadOnlyList<BattleAbilityDefinition> SkillsInOriginalOrder
        {
            get;
        }

        public BattleDefinitions(string variant, string fingerprint, IEnumerable<BattleAbilityDefinition> skills,
            IEnumerable<BattleAbilityDefinition> items, IEnumerable<BattleActorDefinition> actors, IEnumerable<BattleComboDefinition> combos = null)
        {
            if (variant is not ("PAL3" or "PAL3A"))
                throw new ArgumentException("Unknown combat variant.");
            Variant = variant;
            Fingerprint = fingerprint ?? "";
            SkillsInOriginalOrder = BattleAbilityDefinition.Capture(skills);
            Skills = new ReadOnlyDictionary<int, BattleAbilityDefinition>(SkillsInOriginalOrder.ToDictionary(value => value.Id));
            Items = new ReadOnlyDictionary<int, BattleAbilityDefinition>(items.ToDictionary(value => value.Id));
            Actors = new ReadOnlyDictionary<string, BattleActorDefinition>(actors.ToDictionary(value => value.InstanceId, StringComparer.Ordinal));
            BattleComboDefinition[] ordered = combos?.ToArray() ?? Array.Empty<BattleComboDefinition>();
            if (ordered.Any(combo => combo == null) || ordered.GroupBy(combo => combo.Id).Any(group => group.Count() > 1))
                throw new ArgumentException("Combo definitions need unique non-null IDs.", nameof(combos));
            Combos = Array.AsReadOnly(ordered);
        }
    }
}
