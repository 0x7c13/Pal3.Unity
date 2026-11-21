// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Battle
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public sealed class BattleStatusSnapshot
    {
        public BattleState State
        {
            get; internal set;
        }

        public int LatentRounds
        {
            get; internal set;
        }

        public int RemainingRounds
        {
            get; internal set;
        }

        public bool Permanent
        {
            get; internal set;
        }
    }
}
