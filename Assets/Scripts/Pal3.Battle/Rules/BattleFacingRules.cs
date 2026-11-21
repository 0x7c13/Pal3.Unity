// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Battle
{
    using System;

    public static class BattleFacingRules
    {
        public static float Initial(BattleSide side) => side switch
        {
            BattleSide.Ally => 45f,
            BattleSide.Enemy => -135f,
            _ => throw new ArgumentOutOfRangeException(nameof(side)),
        };

        public static float Starting(BattleSide side, BattleEncounter encounter)
        {
            bool reversed = side == BattleSide.Ally && encounter == BattleEncounter.EnemyAmbush ||
                side == BattleSide.Enemy && encounter == BattleEncounter.AllyAmbush;
            return Initial(side) + (reversed ? 180f : 0f);
        }
    }
}
