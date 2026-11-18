// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Battle
{
    using System;
    using System.Collections.Generic;

    public sealed class BattleAttackPlan
    {
        public AttackTiming Timing
        {
            get;
        }

        public BattleMovementPoint Origin
        {
            get;
        }

        public BattleMovementPoint Approach
        {
            get;
        }

        public BattleMovementPoint TargetHome
        {
            get;
        }

        public BattleAttackPlan(AttackTiming timing, BattleMovementPoint origin,
            BattleMovementPoint approach, BattleMovementPoint targetHome)
        {
            Timing = timing ?? throw new ArgumentNullException(nameof(timing));
            Origin = origin;
            Approach = approach;
            TargetHome = targetHome;
        }
    }

    // All coordinates use original GameBox units. Implementations compile immutable
    // resource plans; a running session never changes the shared catalog.
    public interface IBattleStageGeometry
    {
        BattleMovementPoint InitialHome(string actor);

        BattleAttackPlan PlanAttack(string source, string target, BattleMovementPoint sourceHome, BattleMovementPoint targetHome);

        BattleMovementPoint FormationHome(string actor, BattleSlot slot);

        IReadOnlyList<BattleMovementPlan> PlanFormationMove(string actor, BattleMovementPoint currentHome,
            BattleMovementPoint destination, bool passThroughMidpoint);
    }
}
