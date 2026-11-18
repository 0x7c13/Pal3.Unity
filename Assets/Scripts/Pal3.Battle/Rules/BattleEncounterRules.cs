// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Battle
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;

    public enum BattleEncounterOutcome
    {
        None,
        Win,
        Lose,
    }

    // Released CheckPerRound and CheckState use the enemy center's counter,
    // independently of every actor's individual gauge-round counter.
    public sealed class BattleEncounterRules
    {
        private readonly bool _pal3A;

        public BattleEncounterRules(string variant)
        {
            if (variant is not ("PAL3" or "PAL3A"))
                throw new ArgumentException("Unknown combat variant.", nameof(variant));
            _pal3A = variant == "PAL3A";
        }

        public static int NativeMaximumRounds(int roundLimit) => roundLimit == -1 ? int.MaxValue : roundLimit;

        public static bool RoundLimitReached(int bossRounds, int roundLimit) => bossRounds >= NativeMaximumRounds(roundLimit);

        public static bool BeginRound(int nativeIndex, int sequenceRound, int roundLimit, ref int processedRounds, ref int bossRounds)
        {
            if (RoundLimitReached(bossRounds, roundLimit) || processedRounds >= sequenceRound)
                return false;
            processedRounds++;
            if (nativeIndex == 10)
                bossRounds++;
            return true;
        }

        public static int HpAfterMustFail(bool mustFail, int nativeIndex, int hpAfterChange, int maximumHp) =>
            mustFail && nativeIndex == 10 ? maximumHp : hpAfterChange;

        // Native task numbers remain at this boundary because the released
        // variants have different task tables and different outcome gates.
        public BattleEncounterOutcome CheckOutcome(bool allyAlive, bool enemyAlive, int bossRounds, int roundLimit,
            bool mustFail, IReadOnlyCollection<int> unfinishedTasks)
        {
            bool HasTask(int id) => unfinishedTasks != null && Contains(unfinishedTasks, id);
            bool coAttack = HasTask(_pal3A ? 13 : 12);
            if (!allyAlive && !coAttack && (!_pal3A || !HasTask(17) && !HasTask(11)))
                return BattleEncounterOutcome.Lose;
            if ((!enemyAlive || RoundLimitReached(bossRounds, roundLimit) && !mustFail) && !coAttack &&
                (!_pal3A || !HasTask(31) && !HasTask(17)))
                return BattleEncounterOutcome.Win;
            return BattleEncounterOutcome.None;
        }

        private static bool Contains(IReadOnlyCollection<int> values, int value)
        {
            foreach (int entry in values)
                if (entry == value)
                    return true;
            return false;
        }
    }
}
