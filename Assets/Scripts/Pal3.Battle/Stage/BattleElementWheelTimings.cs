// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Battle
{
    // The wheel has no spell name, lineup, casting bar, MP cost or proficiency.
    // Its dedicated body task plays Nangong Huang's z24 and the two linker groups.
    public interface IBattleElementWheelTimings
    {
        AttackTiming ElementWheel(string actor, BattleAbilityDefinition ability);

        BattleAbilityStageProfile ElementWheelStage(string actor, BattleAbilityDefinition ability);

        BattlePhysicalAnimation ElementWheelBody(string actor, BattleAbilityDefinition ability);
    }
}
