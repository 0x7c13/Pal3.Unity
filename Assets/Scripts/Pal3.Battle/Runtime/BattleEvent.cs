// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Battle
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public enum BattleEventKind
    {
        Started, Ready, Command, Hit, Miss, Death, ActionEnded, Cancelled, Interrupted, ControlChanged, Ended,
        ResourceChanged, StatusAdded, StatusRemoved, SkillImproved, SkillLearned, ItemChanged, LevelUp, Escaped,
        FormationOpened, FormationChanged, FormationClosed, ProtectionChanged, MovementStarted, MovementEnded,
        TreasureUseChanged, StealPoolChanged, NumberShown, RelationChanged, CorpseObtained, PresentationCleanupFailed
    }

    public sealed class BattleEvent
    {
        public long Tick
        {
            get; internal set;
        }

        public BattleEventKind Kind
        {
            get; internal set;
        }

        public string Source
        {
            get; internal set;
        }

        public string Target
        {
            get; internal set;
        }

        public long ActionId
        {
            get; internal set;
        }

        public int RequestedDelta
        {
            get; internal set;
        }

        public int ActualDelta
        {
            get; internal set;
        }

        public int HpBefore
        {
            get; internal set;
        }

        public int HpAfter
        {
            get; internal set;
        }

        public bool Critical
        {
            get; internal set;
        }

        public bool Dodged
        {
            get; internal set;
        }

        public uint RandomState
        {
            get; internal set;
        }

        public string Detail
        {
            get; internal set;
        }

        public int Resource { get; internal set; } = -1;

        public int ValueBefore
        {
            get; internal set;
        }

        public int ValueAfter
        {
            get; internal set;
        }

        public int NumberOffset
        {
            get; internal set;
        }

        public float DurationSeconds
        {
            get; internal set;
        }

        public long NumberActivationTick
        {
            get; internal set;
        }

        public float NumberStartedTime
        {
            get; internal set;
        }

        public BattleMovementPoint NumberPosition
        {
            get; internal set;
        }

        public bool NumberIsMiss
        {
            get; internal set;
        }

        public bool NumberIsRecovery
        {
            get; internal set;
        }

        public int AbilityId
        {
            get; internal set;
        }

        public override string ToString() => FormattableString.Invariant($"{Tick * BattleSession.StepSeconds:F2}s {Kind} {Source} → {Target} Δ{ActualDelta} {Detail}");
    }
}
