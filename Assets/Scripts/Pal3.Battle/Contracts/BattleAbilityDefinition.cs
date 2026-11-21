// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Battle
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    // Mutable assembly inputs are consumed once; definitions capture their own immutable collections.
    public sealed class BattleAbilityData
    {
        public int Id;
        public string Name = "";
        public bool IsMagic;
        public int SkillTypeId = -1;
        public int AiNullCureOperation;
        public bool IsItem;
        public bool IsTreasure;
        public bool BattleUsable = true;
        public BattleTargetRange Range;
        public int ElementMask;
        public int SuccessRate = 100;
        public int SpellRank = 1;
        public int LearningLevel = 1;
        public int NextSkillId;
        public int NextSkillRequiredProficiency;
        public int[] UsesBeforeLevelUp = Array.Empty<int>();
        public BattleAttributeEffect[] Attributes = Array.Empty<BattleAttributeEffect>();
        public BattleStatusEffect[] States = Array.Empty<BattleStatusEffect>();
        public BattleCost[] Costs = Array.Empty<BattleCost>();
        public int[] ApplicableIdentities = Array.Empty<int>();
        public string UnsupportedReason = "";
        public int SpecialEffect;
        public BattleSpecialHandler Handler;
        public int ItemType;
        public BattleSkillUnlock[] Unlocks = Array.Empty<BattleSkillUnlock>();
    }

    public sealed class BattleAbilityDefinition
    {
        public int Id
        {
            get;
        }

        public string Name
        {
            get;
        }

        public bool IsMagic
        {
            get;
        }

        public int SkillTypeId
        {
            get;
        }

        public int AiNullCureOperation
        {
            get;
        }

        public bool IsItem
        {
            get;
        }

        public bool IsTreasure
        {
            get;
        }

        public bool BattleUsable
        {
            get;
        }

        public BattleTargetRange Range
        {
            get;
        }

        public int ElementMask
        {
            get;
        }

        public int SuccessRate
        {
            get;
        }

        public int SpellRank
        {
            get;
        }

        public int LearningLevel
        {
            get;
        }

        public int NextSkillId
        {
            get;
        }

        public int NextSkillRequiredProficiency
        {
            get;
        }

        public IReadOnlyList<int> UsesBeforeLevelUp
        {
            get;
        }

        public IReadOnlyList<BattleAttributeEffect> Attributes
        {
            get;
        }

        public IReadOnlyList<BattleStatusEffect> States
        {
            get;
        }

        public IReadOnlyList<BattleCost> Costs
        {
            get;
        }

        public IReadOnlyList<int> ApplicableIdentities
        {
            get;
        }

        public string UnsupportedReason
        {
            get;
        }

        public int SpecialEffect
        {
            get;
        }

        public BattleSpecialHandler Handler
        {
            get;
        }

        public int ItemType
        {
            get;
        }

        public IReadOnlyList<BattleSkillUnlock> Unlocks
        {
            get;
        }

        public bool Revives => States.Any(state => state.State == BattleState.Death && state.Remove);

        public bool Supported => string.IsNullOrEmpty(UnsupportedReason);

        public BattleAbilityDefinition(BattleAbilityData data)
        {
            if (data == null || data.Id <= 0)
                throw new ArgumentException("An ability needs a positive resource ID.");
            Id = data.Id;
            Name = data.Name ?? "";
            IsMagic = data.IsMagic;
            SkillTypeId = data.SkillTypeId;
            AiNullCureOperation = data.AiNullCureOperation;
            IsItem = data.IsItem;
            IsTreasure = data.IsTreasure;
            BattleUsable = data.BattleUsable;
            Range = data.Range;
            ElementMask = data.ElementMask;
            SuccessRate = data.SuccessRate;
            SpellRank = data.SpellRank;
            LearningLevel = data.LearningLevel;
            NextSkillId = data.NextSkillId;
            NextSkillRequiredProficiency = data.NextSkillRequiredProficiency;
            UsesBeforeLevelUp = Capture(data.UsesBeforeLevelUp);
            Attributes = Capture(data.Attributes);
            States = Capture(data.States);
            Costs = Capture(data.Costs);
            ApplicableIdentities = Capture(data.ApplicableIdentities);
            UnsupportedReason = data.UnsupportedReason ?? "";
            SpecialEffect = data.SpecialEffect;
            Handler = data.Handler;
            ItemType = data.ItemType;
            Unlocks = Capture(data.Unlocks);
        }

        internal static IReadOnlyList<T> Capture<T>(IEnumerable<T> values) => Array.AsReadOnly(values?.ToArray() ?? Array.Empty<T>());
    }

    public readonly struct BattleSkillUnlock
    {
        public int SkillId
        {
            get;
        }

        public int OtherSkillId
        {
            get;
        }

        public int OtherTier
        {
            get;
        }

        public int SelfTier
        {
            get;
        }

        public int ActorLevel
        {
            get;
        }

        public BattleSkillUnlock(int skillId, int otherSkillId, int otherTier, int selfTier, int actorLevel)
        {
            SkillId = skillId;
            OtherSkillId = otherSkillId;
            OtherTier = otherTier;
            SelfTier = selfTier;
            ActorLevel = actorLevel;
        }
    }
}
