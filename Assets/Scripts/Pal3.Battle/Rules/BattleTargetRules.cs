// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Battle
{
    using System;
    using System.Linq;

    public static class BattleTargetRules
    {
        // Original GetBestRoleForAttackRowOrCol visits these groups and slots in order.
        private static readonly int[][] EnemyRows = { new[] { 8, 9 }, new[] { 5, 10, 7 }, new[] { 6 } };
        private static readonly int[][] EnemyColumns = { new[] { 5, 8 }, new[] { 6, 10 }, new[] { 7, 9 } };

        private static readonly int[] AllyElementChain = { 1, 2, 4, 3, 5 };

        private static int AllyFormationStart(int sceneElement)
        {
            if (sceneElement is < 0 or > 5)
                throw new ArgumentOutOfRangeException(nameof(sceneElement));
            // FillFiveMagicMatrix leaves its starting index at Water for an unset element.
            return sceneElement == 0 ? 0 : Array.IndexOf(AllyElementChain, BattleEffectRules.GreaterElement(sceneElement));
        }

        public static BattleSlot InitialAllySlot(BattleSlot slot, int sceneElement)
        {
            if (slot == BattleSlot.Center || sceneElement == 0)
                return slot;
            int original = Array.IndexOf(AllyElementChain, (int)slot + 1);
            int offset = AllyFormationStart(sceneElement) + 1;
            return (BattleSlot)(AllyElementChain[(original - offset + 5) % 5] - 1);
        }

        internal static bool IsFrontAllySlot(BattleSlot slot, int sceneElement)
        {
            int start = AllyFormationStart(sceneElement);
            int element = (int)slot + 1;
            return element == AllyElementChain[(start + 2) % 5] || element == AllyElementChain[(start + 3) % 5];
        }

        internal static int BestEnemyLine(bool row, Func<int, bool> available)
        {
            int best = -1, bestCount = 0;
            foreach (int[] line in row ? EnemyRows : EnemyColumns)
            {
                int count = line.Count(available);
                if (count > 0 && count >= bestCount)
                {
                    bestCount = count;
                    best = line.First(available);
                }
            }
            return best;
        }

        public static bool Aligned(BattleSide side, BattleSlot first, BattleSlot second, bool row, int sceneElement)
        {
            if (first == second)
                return true;
            if (side == BattleSide.Enemy)
                return (row ? EnemyRows : EnemyColumns).Any(line => line.Contains(5 + (int)first) && line.Contains(5 + (int)second));
            int start = AllyFormationStart(sceneElement);
            int[] groupsByOffset = row ? new[] { 0, 1, 2, 2, 1 } : new[] { 1, 0, 0, 2, 2 };
            int Group(BattleSlot slot)
            {
                if (slot == BattleSlot.Center)
                    return 1;
                int offset = (Array.IndexOf(AllyElementChain, (int)slot + 1) - start + 5) % 5;
                return groupsByOffset[offset];
            }
            return Group(first) == Group(second);
        }
    }
}
