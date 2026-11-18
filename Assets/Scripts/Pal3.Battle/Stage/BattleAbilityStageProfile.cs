// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Battle
{
    using System;
    using System.Collections.Generic;

    public enum BattleAbilityStageKind
    {
        Skill, Magic, Item, ElementWheel
    }

    public enum BattleAbilityWorkBoundary
    {
        WorkTime, WorkTimeAndBodyIdle, WorkTimeAndFirstGroupFinished
    }

    public enum BattleAbilityStageStep
    {
        Grab, Name, Lineup, PrepareBody, Act, ReturnHome, Release
    }

    // The four original booleans are applied in file order. Later enabled
    // coordinates replace earlier ones; an empty flag set uses the explicit vector.
    public sealed class BattleAbilityEffectBinding
    {
        public int GroupId
        {
            get;
        }

        public float WaitSeconds
        {
            get;
        }

        public int PlacementFlags
        {
            get;
        }

        public BattleMovementPoint ExplicitOrigin
        {
            get;
        }

        public BattleAbilityEffectBinding(int groupId, float waitSeconds, int placementFlags = 0, BattleMovementPoint explicitOrigin = default)
        {
            if (groupId < -1 || !float.IsFinite(waitSeconds) || waitSeconds < 0 || waitSeconds > 600 || placementFlags < 0 || placementFlags > 15)
                throw new ArgumentException("Invalid original ability effect binding.");
            GroupId = groupId;
            WaitSeconds = waitSeconds;
            PlacementFlags = placementFlags;
            ExplicitOrigin = explicitOrigin;
        }

        public BattleMovementPoint Resolve(BattleMovementPoint sourceCenter, BattleMovementPoint targetCenter,
            BattleMovementPoint sourceHome, BattleMovementPoint targetHome, bool hasSingleTarget)
        {
            BattleMovementPoint origin = ExplicitOrigin;
            if ((PlacementFlags & 1) != 0)
                origin = sourceCenter;
            if ((PlacementFlags & 2) != 0)
                origin = targetCenter;
            if ((PlacementFlags & 4) != 0)
                origin = sourceHome;
            if ((PlacementFlags & 8) != 0)
                origin = hasSingleTarget ? targetHome : targetCenter;
            return origin;
        }
    }

    public sealed class BattleAbilityStageProfile
    {
        public BattleAbilityStageKind Kind
        {
            get;
        }

        public BattleAbilityEffectBinding First
        {
            get;
        }

        public BattleAbilityEffectBinding Second
        {
            get;
        }

        public float WorkSeconds
        {
            get;
        }

        public BattleAbilityWorkBoundary WorkBoundary
        {
            get;
        }

        public int BodyAnimationTicks
        {
            get;
        }

        public BattleMovementKind? ApproachKind
        {
            get;
        }

        public BattleMovementKind? ReturnKind
        {
            get;
        }

        public bool TargetCenterIsOpposing
        {
            get;
        }

        public bool RetainsBody
        {
            get;
        }

        public bool NpcItemIdle
        {
            get;
        }

        public float NameSeconds => Kind == BattleAbilityStageKind.Item ? 1.5f : 1f;

        public float LineupSeconds => Kind == BattleAbilityStageKind.Magic ? 2f : 0f;

        public float PrepareWaitSeconds => Kind == BattleAbilityStageKind.Skill ? 0.05f : 0f;

        public BattleAbilityStageProfile(BattleAbilityStageKind kind, BattleAbilityEffectBinding first, BattleAbilityEffectBinding second,
            float workSeconds, BattleAbilityWorkBoundary workBoundary, int bodyAnimationTicks, bool targetCenterIsOpposing,
            BattleMovementKind? approachKind = null, BattleMovementKind? returnKind = null, bool retainsBody = false, bool npcItemIdle = false)
        {
            if (!Enum.IsDefined(typeof(BattleAbilityStageKind), kind) || first == null || second == null ||
                !float.IsFinite(workSeconds) || workSeconds < 0 || workSeconds > 600 ||
                !Enum.IsDefined(typeof(BattleAbilityWorkBoundary), workBoundary) || bodyAnimationTicks < 0 || bodyAnimationTicks > 30000 ||
                approachKind.HasValue && !Enum.IsDefined(typeof(BattleMovementKind), approachKind.Value) ||
                returnKind.HasValue && !Enum.IsDefined(typeof(BattleMovementKind), returnKind.Value))
                throw new ArgumentException("Invalid original ability stage profile.");
            Kind = kind;
            First = first;
            Second = second;
            WorkSeconds = workSeconds;
            WorkBoundary = workBoundary;
            BodyAnimationTicks = bodyAnimationTicks;
            TargetCenterIsOpposing = targetCenterIsOpposing;
            ApproachKind = approachKind;
            ReturnKind = returnKind;
            RetainsBody = retainsBody;
            NpcItemIdle = npcItemIdle;
        }
    }

    public interface IBattleAbilityBodyAnimations
    {
        BattlePhysicalAnimation AbilityBody(string actor, BattleAbilityDefinition ability);

        int? AbilityNativeAction(string actor, BattleAbilityDefinition ability);

        BattlePhysicalAnimation AbilityMovementBody(string actor, BattleAbilityDefinition ability, BattleMovementKind kind, bool returning);
    }

    public interface IBattleAbilityEffects
    {
        BattleAbilityStageProfile AbilityStage(string actor, BattleAbilityDefinition ability);

        IReadOnlyList<BattleMovementPlan> AbilityMovement(BattleActorSnapshot source, BattleActorSnapshot target,
            BattleAbilityDefinition ability, BattleDefinitions definitions, bool returning);
    }
}
