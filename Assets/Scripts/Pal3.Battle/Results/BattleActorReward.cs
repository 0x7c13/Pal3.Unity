// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Battle
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public sealed class BattleActorReward
    {
        public string Actor
        {
            get; internal set;
        }

        public int ExperienceGained
        {
            get; internal set;
        }

        public int LevelBefore
        {
            get; internal set;
        }

        public int LevelAfter
        {
            get; internal set;
        }

        public int ExperienceBefore
        {
            get; internal set;
        }

        public int ExperienceAfter
        {
            get; internal set;
        }

        public IReadOnlyList<int> AttributesBefore
        {
            get; internal set;
        }

        public IReadOnlyList<int> AttributesAfter
        {
            get; internal set;
        }
    }
}
