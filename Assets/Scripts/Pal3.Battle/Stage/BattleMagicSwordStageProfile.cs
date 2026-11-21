// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Battle
{
    using System;

    public interface IBattleMagicSwordTimings
    {
        BattleMagicSwordStageProfile MagicSword(string actor, BattleAbilityDefinition ability);
    }

    // ProcWeaponSkillAct waits for group 1 to finish. WorkTime is deliberately absent.
    public sealed class BattleMagicSwordStageProfile
    {
        public const float NameDelaySeconds = 1;

        public int FirstGroupId
        {
            get;
        }

        public int SecondGroupId
        {
            get;
        }

        public float FirstWaitSeconds
        {
            get;
        }

        public float SecondWaitSeconds
        {
            get;
        }

        public int BodyAnimationTicks
        {
            get;
        }

        public BattleMovementPoint FirstOrigin
        {
            get;
        }

        public BattleMovementPoint SecondOrigin
        {
            get;
        }

        public BattleMagicSwordStageProfile(int firstGroupId, int secondGroupId, float firstWaitSeconds, float secondWaitSeconds, int bodyAnimationTicks,
            BattleMovementPoint firstOrigin = default, BattleMovementPoint secondOrigin = default)
        {
            if (firstGroupId < -1 || secondGroupId < -1 || !float.IsFinite(firstWaitSeconds) || !float.IsFinite(secondWaitSeconds) ||
                firstWaitSeconds < 0 || secondWaitSeconds < 0 || firstWaitSeconds > 600 || secondWaitSeconds > 600 || bodyAnimationTicks < 1 || bodyAnimationTicks > 30000)
                throw new ArgumentException("Invalid original magic sword stage profile.");
            FirstGroupId = firstGroupId;
            SecondGroupId = secondGroupId;
            FirstWaitSeconds = firstWaitSeconds;
            SecondWaitSeconds = secondWaitSeconds;
            BodyAnimationTicks = bodyAnimationTicks;
            FirstOrigin = firstOrigin;
            SecondOrigin = secondOrigin;
        }
    }
}
