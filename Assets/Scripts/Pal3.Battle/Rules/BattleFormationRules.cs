// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Battle
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public sealed class BattleFormationActor
    {
        public int Index
        {
            get;
        }

        public int ResourceId
        {
            get;
        }

        public BattleSlot Slot
        {
            get;
        }

        public int WeaponType
        {
            get;
        }

        public bool Valid
        {
            get;
        }

        public bool Alive
        {
            get;
        }

        public int Hp
        {
            get;
        }

        public int MaxHp
        {
            get;
        }

        public IReadOnlyList<BattleState> States
        {
            get;
        }

        public int TaskCount
        {
            get;
        }

        public bool AtHome
        {
            get;
        }

        public bool Idle
        {
            get;
        }

        public bool Transformed
        {
            get;
        }

        public bool Fled
        {
            get;
        }

        public BattleFormationActor(int index, int resourceId, BattleSlot slot, int weaponType,
            bool valid = true, bool alive = true, int hp = 1, int maxHp = 1,
            IEnumerable<BattleState> states = null, int taskCount = 0, bool atHome = true,
            bool idle = true, bool transformed = false, bool fled = false)
        {
            if (index < 0 || index > 10 || !Enum.IsDefined(typeof(BattleSlot), slot) ||
                weaponType < 0 || weaponType > byte.MaxValue || hp < 0 || maxHp < 0 || taskCount < 0)
                throw new ArgumentException("Invalid formation actor snapshot.");
            Index = index;
            ResourceId = resourceId;
            Slot = slot;
            WeaponType = weaponType;
            Valid = valid;
            Alive = alive;
            Hp = hp;
            MaxHp = maxHp;
            States = Array.AsReadOnly(states?.ToArray() ?? Array.Empty<BattleState>());
            TaskCount = taskCount;
            AtHome = atHome;
            Idle = idle;
            Transformed = transformed;
            Fled = fled;
        }
    }

    public sealed class BattleComboRequirement
    {
        public int Id
        {
            get;
        }

        public int TriggerSkillId
        {
            get;
        }

        public IReadOnlyList<int> Members
        {
            get;
        }

        public IReadOnlyList<int> Positions
        {
            get;
        }

        public IReadOnlyList<int> WeaponTypes
        {
            get;
        }

        // Original FS indices: -1 ignores, 0..25 tests CBRSS(index+1), 26 means any poison.
        public IReadOnlyList<int> AuxiliaryStates
        {
            get;
        }

        public BattleComboRequirement(int id, int triggerSkillId, IEnumerable<int> members,
            IEnumerable<int> positions, IEnumerable<int> weaponTypes, IEnumerable<int> auxiliaryStates)
        {
            Id = id;
            TriggerSkillId = triggerSkillId;
            Members = Capture(members, 4);
            Positions = Capture(positions, 4);
            WeaponTypes = Capture(weaponTypes, 4);
            AuxiliaryStates = Capture(auxiliaryStates, 3);
        }

        private static IReadOnlyList<int> Capture(IEnumerable<int> values, int count)
        {
            int[] result = values?.ToArray() ?? throw new ArgumentNullException(nameof(values));
            if (result.Length != count)
                throw new ArgumentException($"Expected {count} combo conditions.", nameof(values));
            return Array.AsReadOnly(result);
        }
    }

    public sealed class BattleFormationDecision
    {
        public bool Allowed => FailureReason == null;

        public string FailureReason
        {
            get;
        }

        public int? ActorIndex
        {
            get;
        }

        internal BattleFormationDecision(string failureReason = null, int? actorIndex = null)
        {
            FailureReason = failureReason;
            ActorIndex = actorIndex;
        }
    }

    // Eligibility only. The session owns gauge costs, damage routing, action lifecycle and presentation.
    public sealed class BattleFormationRules
    {
        private static readonly BattleFormationDecision Allowed = new();
        private readonly bool _isPal3A;

        public BattleFormationRules(string variant)
        {
            if (variant is not ("PAL3" or "PAL3A"))
                throw new ArgumentException("Unknown combat variant.", nameof(variant));
            _isPal3A = variant == "PAL3A";
        }

        public BattleFormationDecision CanMove(BattleFormationActor actor)
        {
            RequireActor(actor);
            if (!actor.Valid)
                return Denied("角色未出场。", actor.Index);
            if (!actor.Alive)
                return Denied("死亡角色不能换位。", actor.Index);
            foreach (BattleState state in new[] { BattleState.Paralysis, BattleState.Chaos, BattleState.Madness, BattleState.Sleep })
            {
                if (actor.States.Contains(state))
                    return Denied($"角色处于{state}，不能换位。", actor.Index);
            }
            return Allowed;
        }

        public BattleFormationDecision CanChangeLineup(IReadOnlyList<BattleFormationActor> actors)
        {
            if (actors == null)
                throw new ArgumentNullException(nameof(actors));
            foreach (BattleFormationActor actor in actors)
            {
                RequireActor(actor);
                if (actor.Index < 5 && actor.Valid && actor.TaskCount > 0)
                    return Denied("我方角色仍有未结束的舞台任务。", actor.Index);
            }
            return Allowed;
        }

        internal bool IsProtectionTargetSlot(int index) => index >= 1 && index <= (_isPal3A ? 2 : 3);

        public BattleFormationDecision CanSetProtect(BattleFormationActor guardian, BattleFormationActor target)
        {
            RequireActor(guardian);
            RequireActor(target);
            if (guardian.Index != 0 || !guardian.Valid || !guardian.Alive)
                return Denied("只有出场且存活的原版角色槽0可以设置援护。", guardian.Index);
            if (guardian.Slot != BattleSlot.Center)
                return Denied("援护者必须位于中位才能设置援护。", guardian.Index);
            if (!IsProtectionTargetSlot(target.Index))
                return Denied("原版援护面板不提供该角色。", target.Index);
            if (!target.Valid || !target.Alive)
                return Denied("援护对象必须出场且存活。", target.Index);
            return Allowed;
        }

        public BattleFormationDecision CanProtect(int selectedTarget, BattleFormationActor guardian, BattleFormationActor target)
        {
            RequireActor(guardian);
            RequireActor(target);
            if (guardian.Index != 0)
                return Denied("只有原版角色槽0执行援护。", guardian.Index);
            if (selectedTarget == 0 || selectedTarget != target.Index)
                return Denied("目标不是当前设定的援护对象。", target.Index);
            if (!guardian.Valid || !guardian.Alive)
                return Denied("援护者未出场或已经死亡。", guardian.Index);
            if (guardian.Hp < guardian.MaxHp / 2)
                return Denied("援护者的精不足最大精的一半。", guardian.Index);
            if (target.Hp > target.MaxHp / 10)
                return Denied("援护对象的精高于最大精的十分之一。", target.Index);
            if (guardian.Slot != BattleSlot.Center)
                return Denied("援护者必须位于中央。", guardian.Index);
            if (!guardian.Idle)
                return Denied("援护者尚未空闲。", guardian.Index);
            if (!guardian.AtHome)
                return Denied("援护者尚未回到阵位。", guardian.Index);
            if (!target.AtHome)
                return Denied("援护对象尚未回到阵位。", target.Index);
            return Allowed;
        }

        public BattleFormationDecision CanCoAttack(int initiator, int triggerSkillId, BattleComboRequirement requirement,
            IReadOnlyList<BattleFormationActor> actors)
        {
            if (requirement == null || actors == null)
                throw new ArgumentNullException(requirement == null ? nameof(requirement) : nameof(actors));
            if (initiator < 0 || initiator >= 5)
                return Denied("合击只能由我方角色发起。", initiator);
            if (triggerSkillId != requirement.TriggerSkillId)
                return Denied($"需要触发技能{requirement.TriggerSkillId}。");
            var byIndex = actors.ToDictionary(actor => actor.Index);
            if (!byIndex.TryGetValue(initiator, out BattleFormationActor source) || source.ResourceId != requirement.Members[0])
                return Denied($"需要资源角色{requirement.Members[0]}发起合击。", initiator);
            for (int member = 0; member < 4; member++)
            {
                if (requirement.Members[member] == 0)
                    continue;
                int index = MemberIndex(requirement.Members[member]);
                if (index < 0)
                    return Denied($"未核实成员资源{requirement.Members[member]}的原版身份映射。");
                if (!byIndex.TryGetValue(index, out BattleFormationActor actor) || !actor.Valid)
                    return Denied($"合击成员{requirement.Members[member]}未出场。", index);
                if (_isPal3A && actor.Transformed)
                    return Denied("变身中的成员不能参与外传合击。", index);
                int position = requirement.Positions[member];
                int actual = (int)actor.Slot + 1;
                if (position >= 1 && position <= 6 && position != actual)
                    return Denied($"合击成员必须位于原版阵位{position}。", index);
                if (position == 7 && actual is not (2 or 3 or 4))
                    return Denied("合击成员必须位于火、风或雷阵位。", index);
                if (position == 8 && actual is not (1 or 2 or 5))
                    return Denied("合击成员必须位于水、火或土阵位。", index);
                int weapon = requirement.WeaponTypes[member];
                if ((!_isPal3A || weapon > 0) && actor.WeaponType != weapon)
                    return Denied($"合击成员需要武器类型{weapon}。", index);
            }
            for (int auxiliary = 0; auxiliary < 3; auxiliary++)
            {
                int index = MemberIndex(requirement.Members[auxiliary + 1]);
                if (index < 0)
                    continue;
                BattleFormationActor actor = byIndex[index];
                int condition = requirement.AuxiliaryStates[auxiliary];
                if (condition >= 0 && condition < 26 && !actor.States.Contains((BattleState)condition))
                    return Denied($"合击成员需要状态{(BattleState)condition}。", index);
                if (condition == 26 && !actor.States.Any(state => state >= BattleState.PoisonWind && state <= BattleState.PoisonEarth))
                    return Denied("合击成员需要任一五毒状态。", index);
            }
            return Allowed;
        }

        internal int MemberIndex(int resourceId)
        {
            if (_isPal3A)
                return resourceId >= 101 && resourceId <= 105 ? resourceId - 101 : -1;
            return resourceId switch
            {
                101 => 0,
                104 => 1,
                105 or 106 => 2,
                107 => 3,
                102 => 4,
                _ => -1,
            };
        }

        private static BattleFormationDecision Denied(string reason, int? actor = null) => new(reason, actor);

        private static void RequireActor(BattleFormationActor actor)
        {
            if (actor == null)
                throw new ArgumentNullException(nameof(actor));
        }
    }
}
