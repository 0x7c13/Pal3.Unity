// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Battle
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public sealed class BattleComboDefinition
    {
        public int Id => Requirement.Id;

        public string Name
        {
            get;
        }

        public BattleComboRequirement Requirement
        {
            get;
        }

        public int NativeRange
        {
            get;
        }

        public IReadOnlyList<int> ResourceValues
        {
            get;
        }

        public int PowerMultiplierPercentage
        {
            get;
        }

        public BattleComboDefinition(string name, BattleComboRequirement requirement, int originalRange,
            IEnumerable<int> originalResourceValues, int powerMultiplierPercentage = 0)
        {
            Requirement = requirement ?? throw new ArgumentNullException(nameof(requirement));
            Name = name ?? "";
            NativeRange = originalRange;
            int[] values = originalResourceValues?.ToArray() ?? throw new ArgumentNullException(nameof(originalResourceValues));
            if (values.Length != 12 || originalRange < 0 || originalRange > 3)
                throw new ArgumentException("An original combo requires twelve raw resource slots and a verified range.");
            ResourceValues = Array.AsReadOnly(values);
            PowerMultiplierPercentage = powerMultiplierPercentage;
        }
    }

    public interface IBattleCoAttackTimings
    {
        BattleComboStageProfile CoAttack(string actor, BattleComboDefinition combo);

        BattleMovementPoint CoAttackHome(string actor, BattleSlot slot);
    }
}
