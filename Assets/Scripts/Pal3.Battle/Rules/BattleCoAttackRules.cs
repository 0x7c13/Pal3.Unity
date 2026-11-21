// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Battle
{
    using System;
    using System.Collections.Generic;

    // Arithmetic only. Eligibility, achievement registration, resource writes and
    // stage/thread lifecycle remain with their respective domain policies.
    public sealed class BattleCoAttackRules
    {
        private readonly bool _isPal3A;

        public BattleCoAttackRules(string variant)
        {
            if (variant is not ("PAL3" or "PAL3A"))
                throw new ArgumentException("Unknown combat variant.", nameof(variant));
            _isPal3A = variant == "PAL3A";
        }

        // The callback runs once per nonzero man[] in stored order. It must execute
        // original NormalAttackDamage with range=0, including that call's RNG.
        public int Power(IReadOnlyList<int> participantIds, Func<int, int> normalDamage,
            int multiplierPercentage, BattleRandom random)
        {
            if (participantIds == null || normalDamage == null || random == null)
                throw new ArgumentNullException(participantIds == null ? nameof(participantIds)
                    : normalDamage == null ? nameof(normalDamage) : nameof(random));
            if (participantIds.Count != 4)
                throw new ArgumentException("Original combo man[] contains four entries.", nameof(participantIds));
            int sum = 0;
            for (int member = 0; member < 4; member++)
            {
                if (participantIds[member] != 0)
                    sum = unchecked(sum + normalDamage(participantIds[member]));
            }
            if (!_isPal3A)
                sum = unchecked(sum * 2);
            else if (multiplierPercentage > 0)
            {
                float multiplier = (float)(multiplierPercentage * (double)0.01f);
                int integerMultiplier = (int)Math.Round(multiplier, MidpointRounding.ToEven);
                sum = unchecked(sum * integerMultiplier);
            }
            return random.Warp(sum, 0.1f);
        }

        // PAL3A evaluates even the nine discarded non-resource entries, preserving
        // their NormalAttackDamage/Warp RNG calls. Only indices 0..2 write resources.
        public IReadOnlyList<int> EvaluationIndices(IReadOnlyList<int> rawRoleValues)
        {
            if (rawRoleValues == null)
                throw new ArgumentNullException(nameof(rawRoleValues));
            if (rawRoleValues.Count != 12)
                throw new ArgumentException("Original RoleValue contains twelve entries.", nameof(rawRoleValues));
            var result = new List<int>();
            for (int index = 0; index < 12; index++)
            {
                if (_isPal3A || rawRoleValues[index] != 0)
                    result.Add(index);
            }
            return result.AsReadOnly();
        }

        public static bool WritesResource(int index) => index >= 0 && index < 3;

        public static int SignedPower(int power, int rawRoleValue)
        {
            int magnitude = power < 0 ? unchecked(-power) : power;
            return rawRoleValue > 0 ? magnitude : unchecked(-magnitude);
        }
    }
}
