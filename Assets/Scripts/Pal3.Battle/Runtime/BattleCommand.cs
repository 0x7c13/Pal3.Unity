// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Battle
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public enum BattleAction
    {
        Attack, Defend, Skill, Item, Flee, Transform
    }

    public readonly struct BattleCommand
    {
        public string Actor
        {
            get;
        }

        public BattleAction Action
        {
            get;
        }

        public string Target
        {
            get;
        }

        public int AbilityId
        {
            get;
        }

        public BattleCommand(string actor, BattleAction action, string target = null, int abilityId = 0)
        {
            Actor = actor;
            Action = action;
            Target = target;
            AbilityId = abilityId;
        }
    }
}
