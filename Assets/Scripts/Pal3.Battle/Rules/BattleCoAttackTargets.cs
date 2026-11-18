// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Battle
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public sealed class BattleCoAttackTargetActorSnapshot
    {
        public bool Valid
        {
            get;
        }

        public bool Alive
        {
            get;
        }

        public bool Fled
        {
            get;
        }

        public bool Boss
        {
            get;
        }

        public int CurrentHp
        {
            get;
        }

        public BattleCoAttackTargetActorSnapshot(bool valid, bool alive, bool fled, bool boss, int currentHp)
        {
            Valid = valid;
            Alive = alive;
            Fled = fled;
            Boss = boss;
            CurrentHp = currentHp;
        }
    }

    // Collection positions are original native actor indices 0..10, not lineup positions.
    public sealed class BattleCoAttackTargetInput
    {
        public int NativeTargetIndex
        {
            get;
        }

        public IReadOnlyList<bool> PreviousTargetMask
        {
            get;
        }

        public int RawRange
        {
            get;
        }

        public IReadOnlyList<BattleCoAttackTargetActorSnapshot> Actors
        {
            get;
        }

        public BattleCoAttackTargetInput(int nativeTargetIndex, IEnumerable<bool> previousTargetMask,
            int rawRange, IEnumerable<BattleCoAttackTargetActorSnapshot> actors)
        {
            if (nativeTargetIndex < -1 || nativeTargetIndex > 10)
                throw new ArgumentOutOfRangeException(nameof(nativeTargetIndex));
            if (rawRange < 0 || rawRange > 3)
                throw new ArgumentOutOfRangeException(nameof(rawRange));
            bool[] mask = previousTargetMask?.ToArray() ?? throw new ArgumentNullException(nameof(previousTargetMask));
            BattleCoAttackTargetActorSnapshot[] capturedActors = actors?.ToArray() ?? throw new ArgumentNullException(nameof(actors));
            if (mask.Length != 11 || capturedActors.Length != 11 || capturedActors.Any(actor => actor == null))
                throw new ArgumentException("Original combo targeting requires eleven actor slots and mask entries.");
            NativeTargetIndex = nativeTargetIndex;
            PreviousTargetMask = Array.AsReadOnly(mask);
            RawRange = rawRange;
            Actors = Array.AsReadOnly(capturedActors);
        }
    }

    public sealed class BattleCoAttackTargetSelection
    {
        public int NativeTargetIndex
        {
            get;
        }

        public bool Multi
        {
            get;
        }

        // Single-target expansion retains the old native mask, even though only
        // NativeTargetIndex is effective. Keeping it is necessary for exact replay.
        public IReadOnlyList<bool> TargetMask
        {
            get;
        }

        public IReadOnlyList<int> TargetIndices
        {
            get;
        }

        internal BattleCoAttackTargetSelection(int nativeTargetIndex, bool multi, bool[] mask)
        {
            NativeTargetIndex = nativeTargetIndex;
            Multi = multi;
            TargetMask = Array.AsReadOnly(mask);
            int[] indices = multi ? Enumerable.Range(0, 11).Where(index => mask[index]).ToArray()
                : nativeTargetIndex < 0 ? Array.Empty<int>() : new[] { nativeTargetIndex };
            TargetIndices = Array.AsReadOnly(indices);
        }
    }

    // Original GetCoAttackRange: both published variants use the same policy.
    // Eligibility and the native-index-to-session-identity mapping belong to the caller.
    public sealed class BattleCoAttackTargets
    {
        public BattleCoAttackTargets(string variant)
        {
            if (variant is not ("PAL3" or "PAL3A"))
                throw new ArgumentException("Unknown combat variant.", nameof(variant));
        }

        public BattleCoAttackTargetSelection Resolve(BattleCoAttackTargetInput input)
        {
            if (input == null)
                throw new ArgumentNullException(nameof(input));
            int target = input.NativeTargetIndex;
            bool[] mask = input.PreviousTargetMask.ToArray();
            if (input.RawRange is 1 or 3)
            {
                Array.Clear(mask, 0, mask.Length);
                int first = input.RawRange == 1 ? 0 : 5;
                int end = input.RawRange == 1 ? 5 : 11;
                for (int index = first; index < end; index++)
                {
                    BattleCoAttackTargetActorSnapshot actor = input.Actors[index];
                    mask[index] = actor.Valid && !actor.Fled;
                }
                return new BattleCoAttackTargetSelection(-1, true, mask);
            }
            if (input.RawRange == 2)
            {
                if (target >= 0 && target < 5)
                    target = SelectEnemy(input.Actors);
                if (target == -1)
                    target = Array.FindIndex(mask, selected => selected);
            }
            return new BattleCoAttackTargetSelection(target, false, mask);
        }

        private static int SelectEnemy(IReadOnlyList<BattleCoAttackTargetActorSnapshot> actors)
        {
            // Original locals initialize both HP and best index to zero. With no
            // eligible positive-HP enemy, the result is native player slot 0.
            int best = 0;
            int highestHp = 0;
            for (int index = 5; index < 11; index++)
            {
                BattleCoAttackTargetActorSnapshot actor = actors[index];
                if (!actor.Valid || !actor.Alive || actor.Fled)
                    continue;
                if (actor.Boss)
                    return index;
                if (actor.CurrentHp > highestHp)
                {
                    highestHp = actor.CurrentHp;
                    best = index;
                }
            }
            return best;
        }
    }
}
