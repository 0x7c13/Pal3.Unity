// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Battle
{
    using System;
    using System.Collections.Generic;

    public readonly struct BattleNativeEnemySlot
    {
        public bool Valid
        {
            get;
        }

        public bool Alive
        {
            get;
        }

        public bool Escaped
        {
            get;
        }

        public BattleNativeEnemySlot(bool valid, bool alive, bool escaped = false)
        {
            Valid = valid;
            Alive = alive;
            Escaped = escaped;
        }
    }

    public readonly struct BattleMonsterRequest
    {
        public int NativeIndex
        {
            get;
        }

        public int ActorId
        {
            get;
        }

        public BattleMonsterRequest(int nativeIndex, int actorId)
        {
            if (nativeIndex < 5 || nativeIndex > 10 || actorId <= 0)
                throw new ArgumentOutOfRangeException(nameof(nativeIndex));
            NativeIndex = nativeIndex;
            ActorId = actorId;
        }
    }

    public sealed class BattleBossRules
    {
        private readonly bool _pal3A;

        public BattleBossRules(string variant)
        {
            if (variant is not ("PAL3" or "PAL3A"))
                throw new ArgumentException("Unknown combat variant.", nameof(variant));
            _pal3A = variant == "PAL3A";
        }

        public bool SelfDestructsAfterMagic(int actorId, int magicId) => actorId == 7312 && magicId == (_pal3A ? 308 : 210);

        public bool SkipsMagicCost(int actorId) => _pal3A && actorId == 7345;

        public int DeathReplacement(int actorId) => !_pal3A && actorId == 738 ? 739 : 0;

        public IReadOnlyList<BattleState> RoundStates(int actorId, int processedRound) => actorId == 7372 && processedRound == 3
            ? _pal3A ? Array.AsReadOnly(new[] { BattleState.AttackIncrease, BattleState.SpeedIncrease, BattleState.DefenseIncrease })
                : Array.AsReadOnly(new[] { BattleState.AttackIncrease })
            : Array.Empty<BattleState>();

        public int SplitSlot(int actorId, int processedRound, IReadOnlyList<BattleNativeEnemySlot> slots, BattleRandom random)
        {
            if (slots == null || slots.Count != 6 || random == null)
                throw new ArgumentException("Six original enemy slots and a random stream are required.");
            int probability = actorId == 7378 && processedRound >= 3 ? 5 : actorId == 7395 && processedRound == 2 ? 20 : 0;
            if (!random.Probability(probability))
                return -1;
            for (int offset = 0; offset < slots.Count; offset++)
                if (!slots[offset].Valid || actorId == 7378 && !slots[offset].Alive)
                    return offset + 5;
            return -1;
        }

        public IReadOnlyList<BattleMonsterRequest> Summon(int sourceIndex, int aliveEnemies, IReadOnlyList<BattleNativeEnemySlot> slots, BattleRandom random)
        {
            if (slots == null || slots.Count != 6 || random == null)
                throw new ArgumentException("Six original enemy slots and a random stream are required.");
            if (aliveEnemies != 1)
                return Array.Empty<BattleMonsterRequest>();
            if (_pal3A)
            {
                int first = 5;
                int second = 7;
                for (int attempt = 0; attempt < 256; attempt++)
                {
                    int candidateFirst = random.Next() % 5 + 6;
                    int candidateSecond = random.Next() % 5 + 6;
                    if (candidateFirst == sourceIndex || candidateSecond == sourceIndex || candidateFirst == candidateSecond)
                        continue;
                    first = candidateFirst;
                    second = candidateSecond;
                    break;
                }
                return Array.AsReadOnly(new[] { new BattleMonsterRequest(first, 7364), new BattleMonsterRequest(second, 7365) });
            }
            if (slots[0].Alive && !slots[0].Escaped || slots[2].Alive && !slots[2].Escaped)
                return Array.Empty<BattleMonsterRequest>();
            int[] identities = { 7369, 7368, 7370 };
            for (int attempt = 0; attempt < 16; attempt++)
            {
                int first = random.Next() % 3;
                int second = random.Next() % 3;
                if (first != second)
                    (identities[first], identities[second]) = (identities[second], identities[first]);
            }
            return Array.AsReadOnly(new[] { new BattleMonsterRequest(5, identities[0]), new BattleMonsterRequest(7, identities[1]) });
        }
    }
}
