// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Battle
{
    using System;

    public static class BattleAiControlRules
    {
        public static bool ConsumesGauges(int before, int after)
        {
            if (before is < 0 or > 63 || after is < 0 or > 63)
                throw new ArgumentOutOfRangeException(before is < 0 or > 63 ? nameof(before) : nameof(after));
            int difference = before ^ after;
            // Published ProcAIList: Attack is not part of the cost comparison.
            // Disabled child settings still commit, without generating ProcAI.
            return (difference & 32) != 0 || (after & 32) != 0 && (difference & 30) != 0;
        }
    }
}
