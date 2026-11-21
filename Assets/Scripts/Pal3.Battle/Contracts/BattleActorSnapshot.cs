// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Battle
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public sealed class BattleActorSnapshot
    {
        public int ActorId
        {
            get; internal set;
        }

        public int NativeIndex
        {
            get; internal set;
        }

        public int CorpseItem
        {
            get; internal set;
        }

        public int Love
        {
            get; internal set;
        }

        public long NativePresentationTick
        {
            get; internal set;
        }

        public float NativePlaybackRate
        {
            get; internal set;
        }

        public float WeaponTrailSeconds
        {
            get; internal set;
        }

        public float? NativeAnimationHalfDepth
        {
            get; internal set;
        }

        public string NativeAnimationClip
        {
            get; internal set;
        }

        public double NativeAnimationSeconds
        {
            get; internal set;
        }

        public string Id
        {
            get; internal set;
        }

        public BattleSide Side
        {
            get; internal set;
        }

        public BattleSlot Slot
        {
            get; internal set;
        }

        public BattleMovementPoint Home
        {
            get; internal set;
        }

        public BattleMovementPoint Position
        {
            get; internal set;
        }

        public BattleMovementSnapshot Movement
        {
            get; internal set;
        }

        public BattleAttackPlan AttackPlan
        {
            get; internal set;
        }

        public int Hp
        {
            get; internal set;
        }

        public int MaxHp
        {
            get; internal set;
        }

        public int Sp
        {
            get; internal set;
        }

        public int Mp
        {
            get; internal set;
        }

        public int Gauge
        {
            get; internal set;
        }

        public int CastGauge
        {
            get; internal set;
        }

        public int MaxSp
        {
            get; internal set;
        }

        public int MaxMp
        {
            get; internal set;
        }

        public int Level
        {
            get; internal set;
        }

        public int Experience
        {
            get; internal set;
        }

        public IReadOnlyList<int> Attributes
        {
            get; internal set;
        }

        public IReadOnlyList<BattleStatusSnapshot> States
        {
            get; internal set;
        }

        public float FacingDegrees
        {
            get; internal set;
        }

        public BattleMagicSwordSnapshot MagicSword
        {
            get; internal set;
        }

        public IReadOnlyList<BattleSkillSnapshot> Skills
        {
            get; internal set;
        }

        public BattleAction? CurrentAction
        {
            get; internal set;
        }

        public int AbilityId
        {
            get; internal set;
        }

        public int Rounds
        {
            get; internal set;
        }

        public bool Ready
        {
            get; internal set;
        }

        public bool Guarding
        {
            get; internal set;
        }

        public bool Fleeing
        {
            get; internal set;
        }

        public bool Escaped
        {
            get; internal set;
        }

        public bool AliveFlag
        {
            get; internal set;
        }

        public bool ModelAliveFlag
        {
            get; internal set;
        }

        public bool Alive => AliveFlag && !Escaped;

        public BattlePose Pose
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

        public int ActionTick
        {
            get; internal set;
        }

        public int PoseTick
        {
            get; internal set;
        }
    }
}
