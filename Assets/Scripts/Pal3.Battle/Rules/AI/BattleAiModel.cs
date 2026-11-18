// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Battle
{
    using System;
    using System.Collections.Generic;

    public enum BattleAiDemandKind
    {
        Null, Recover, Revive, Detoxification, BadSpecStateRecover, BeAttack, OneHit, FiveMagicAttack, WallMirrorAttack, BoundAttack
    }

    public enum BattleAiAttemptKind
    {
        Normal, RecoverMagic, RecoverBigMagic, AttackMagic, AttackBigMagic, RandomMiscMagic, SelectedMiscMagic,
        RecoverSkill, RecoverBigSkill, AttackSkill, AttackBigSkill, RecoverItem, RecoverBigItem, AttackItem, AttackBigItem,
        ReviveMagic, ReviveSkill, ReviveItem, CureMagic, CureSkill, CureItem, BossPhysical, FiveSpiritMagic, FiveSpiritBigMagic, SpecAI
    }

    public enum BattleAiStatusQuery
    {
        Debuff, Buff, Poison
    }

    public sealed class BattleAiProfile
    {
        public bool Attack, Magic, Skill, Item, Negative, Enabled;

        public void SetEnabled(bool enabled)
        {
            Attack = Magic = Skill = Item = Enabled = enabled;
        }
    }

    // A captured entry in original directory order: monster slot order or player magic-grid order.
    public sealed class BattleAiSkill
    {
        public int Id, Type, Range, Power, PowerMode, ElementMask, NullCureOperation, Level = 1, Count = 1;
        public bool Mastered = true;
        public bool IsMagic = true;
        public bool IsItem;
        public int[] StateOperations = Array.Empty<int>();
    }

    // Index is implicit in Roles[0..10]; never compact the original identity/formation positions.
    public sealed class BattleAiRole
    {
        public bool Valid, Alive, Fled, Fleeing, EnemyFriend, Boss;
        public bool Chaos, Madness, Seal, BadState;
        public int Identity, Hp, MaxHp, Mp, MaxMp, WorstBadState;
        public BattleAiProfile Profile = new();
        public IReadOnlyList<BattleAiSkill> Skills = Array.Empty<BattleAiSkill>();
    }

    public sealed class BattleAiDemand
    {
        public int Satisfy = 100;
        public long ActionId = -1;
        public bool Handled;
        public int State;
    }

    public readonly struct BattleAiAttempt
    {
        public BattleAiAttemptKind Kind
        {
            get;
        }

        public int Source
        {
            get;
        }

        public int Target
        {
            get;
        }

        public int AbilityId
        {
            get;
        }

        public BattleAiDemand Demand
        {
            get;
        }

        public BattleAiAttempt(BattleAiAttemptKind kind, int source, int target = -1, BattleAiDemand demand = null, int abilityId = 0)
        {
            Kind = kind;
            Source = source;
            Target = target;
            Demand = demand;
            AbilityId = abilityId;
        }
    }

    // Attempts are synchronous, sequential Cast... boundaries. A failed attempt may consume the
    // shared random stream. The adapter owns cost, status, formation, range and command submission.
    public abstract class BattleAiPort
    {
        public abstract long TryAttempt(BattleAiAttempt attempt);

        public virtual bool CanCast(int source, int skillId) => false;

        public virtual bool CanAttackBack(int source, int target) => true;

        public virtual bool IsActionActive(long actionId) => false;

        public virtual int SceneFiveSpiritMode => 0;

        // Original force numbers: 0 unset, 1 player side, 2 enemy side.
        public virtual int SceneFiveSpiritOwner => 0;

        public virtual void RecordPlayerNormal(int source, int target)
        {
        }

        public virtual bool CanRepeat(int source, BattleAiCommand command) => false;

        public virtual bool AllRepeatTargetsDead(BattleAiCommand command) => true;

        public virtual bool IsRepeatAttack(BattleAiCommand command) => true;

        public virtual int PickRepeatTarget(int source, bool enemy) => -1;

        public virtual int PickRepeatTarget(int source, bool enemy, bool filterBackRole) => PickRepeatTarget(source, enemy);

        public virtual void ExpandRepeatRange(BattleAiCommand command)
        {
        }

        public virtual long SubmitRepeat(BattleAiCommand command) => -1;
    }

    public sealed class BattleAiCommand
    {
        // Original command constants: 0x103 attack, 0x104 skill, 0x105 magic,
        // 0x106 item, 0x109 defend, 0x10b flee; PAL3A additionally handles 0x10f.
        public int Kind, AbilityId, Source, Target;
        public bool MultipleTargets;
        public bool[] TargetMask = new bool[11];
    }
}
