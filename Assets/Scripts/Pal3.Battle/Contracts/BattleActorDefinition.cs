// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Battle
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public sealed class BattleActorData
    {
        public string InstanceId;
        public int Identity = -1;
        public BattleMagicSwordSnapshot MagicSword;
        public bool Boss;
        public BattleEquipmentDefinition[] Equipment = Array.Empty<BattleEquipmentDefinition>();
        public BattleState[] ImmuneStates = Array.Empty<BattleState>();
        public BattleState[] PermanentStates = Array.Empty<BattleState>();
        public BattleState[] InitialStates = Array.Empty<BattleState>();
        public int PhysicalAttackElements;
        public int PhysicalDefenseElements;
        public int[] PhysicalAttackElementValues = new int[5];
        public int[] PhysicalDefenseElementValues = new int[5];
        public int NormalAttackMode;
        public BattleTargetRange NormalAttackRange = BattleTargetRange.OpponentSingle;
        public int ExperienceReward;
        public int MoneyReward;
        public int LootItem;
        public int LootCount;
        public int CorpseItem;
        public int CorpseSkill;
        public int CorpsePercent;
        public int EscapePercent;
        public int StealableMoney;
        public int StealableItemId;
        public int StealableItemCount;
        public bool MoneyShield;
        public int[] ExperienceThresholds = Array.Empty<int>();
        public int[][] AttributesByLevel = Array.Empty<int[]>();
        public string GrowthUnsupportedReason = "";
        public string UnsupportedReason = "";
    }

    public sealed class BattleActorDefinition
    {
        public string InstanceId
        {
            get;
        }

        public int Identity
        {
            get;
        }

        public bool Boss
        {
            get;
        }

        public BattleMagicSwordSnapshot MagicSword
        {
            get;
        }

        public IReadOnlyList<BattleEquipmentDefinition> Equipment
        {
            get;
        }

        public IReadOnlyList<BattleState> ImmuneStates
        {
            get;
        }

        public IReadOnlyList<BattleState> PermanentStates
        {
            get;
        }

        public IReadOnlyList<BattleState> InitialStates
        {
            get;
        }

        public int PhysicalAttackElements
        {
            get;
        }

        public int PhysicalDefenseElements
        {
            get;
        }

        public IReadOnlyList<int> PhysicalAttackElementValues
        {
            get;
        }

        public IReadOnlyList<int> PhysicalDefenseElementValues
        {
            get;
        }

        public int NormalAttackMode
        {
            get;
        }

        public BattleTargetRange NormalAttackRange
        {
            get;
        }

        public int ExperienceReward
        {
            get;
        }

        public int MoneyReward
        {
            get;
        }

        public int LootItem
        {
            get;
        }

        public int LootCount
        {
            get;
        }

        public int CorpseItem
        {
            get;
        }

        public int CorpseSkill
        {
            get;
        }

        public int CorpsePercent
        {
            get;
        }

        public int EscapePercent
        {
            get;
        }

        public int StealableMoney
        {
            get;
        }

        public int StealableItemId
        {
            get;
        }

        public int StealableItemCount
        {
            get;
        }

        public bool MoneyShield
        {
            get;
        }

        public IReadOnlyList<int> ExperienceThresholds
        {
            get;
        }

        public IReadOnlyList<IReadOnlyList<int>> AttributesByLevel
        {
            get;
        }

        public string GrowthUnsupportedReason
        {
            get;
        }

        public string UnsupportedReason
        {
            get;
        }

        public BattleActorDefinition(BattleActorData data)
        {
            if (data == null || string.IsNullOrWhiteSpace(data.InstanceId))
                throw new ArgumentException("Missing actor instance ID.");
            if (data.StealableMoney < 0 || data.StealableItemId < 0 || data.StealableItemCount < 0)
                throw new ArgumentException("Invalid initial stealable pool.");
            InstanceId = data.InstanceId;
            Identity = data.Identity;
            MagicSword = data.MagicSword;
            Boss = data.Boss;
            Equipment = BattleAbilityDefinition.Capture(data.Equipment);
            ImmuneStates = BattleAbilityDefinition.Capture(data.ImmuneStates);
            PermanentStates = BattleAbilityDefinition.Capture(data.PermanentStates);
            InitialStates = BattleAbilityDefinition.Capture(data.InitialStates);
            PhysicalAttackElements = data.PhysicalAttackElements;
            PhysicalDefenseElements = data.PhysicalDefenseElements;
            PhysicalAttackElementValues = BattleAbilityDefinition.Capture(data.PhysicalAttackElementValues);
            PhysicalDefenseElementValues = BattleAbilityDefinition.Capture(data.PhysicalDefenseElementValues);
            NormalAttackMode = data.NormalAttackMode;
            NormalAttackRange = data.NormalAttackRange;
            ExperienceReward = data.ExperienceReward;
            MoneyReward = data.MoneyReward;
            LootItem = data.LootItem;
            LootCount = data.LootCount;
            CorpseItem = data.CorpseItem;
            CorpseSkill = data.CorpseSkill;
            CorpsePercent = data.CorpsePercent;
            EscapePercent = data.EscapePercent;
            StealableMoney = data.StealableMoney;
            StealableItemId = data.StealableItemId;
            StealableItemCount = data.StealableItemCount;
            MoneyShield = data.MoneyShield;
            ExperienceThresholds = BattleAbilityDefinition.Capture(data.ExperienceThresholds);
            AttributesByLevel = Array.AsReadOnly((data.AttributesByLevel ?? Array.Empty<int[]>()).Select(values => BattleAbilityDefinition.Capture(values)).ToArray());
            GrowthUnsupportedReason = data.GrowthUnsupportedReason ?? "";
            UnsupportedReason = data.UnsupportedReason ?? "";
        }
    }
}
