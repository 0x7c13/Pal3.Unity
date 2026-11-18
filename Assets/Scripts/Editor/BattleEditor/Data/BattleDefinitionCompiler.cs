// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.BattleEditor
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Battle;
    using Core.Contract.Constants;
    using Core.Contract.Enums;
    using Core.DataReader.Gdb;

    public static partial class BattleDefinitionCompiler
    {
        public static BattleDefinitions Build(BattleData data, BattleSetup setup)
        {
            if (data == null || setup == null)
                throw new ArgumentNullException(data == null ? nameof(data) : nameof(setup));
            BattleScenario scenario = setup.CreateInstance();
            if (scenario.Variant != BattleData.Variant)
                throw new ArgumentException("The scenario and original database variants differ.", nameof(setup));
            BattleMagicSwordSnapshot cultivation = scenario.Variant == "PAL3"
                ? (scenario.Participants.FirstOrDefault(actor => actor.Side == BattleSide.Ally && BattleData.PlayerIdentity(actor.ActorId) == 0)?.MagicSword
                    ?? new BattleMagicSword()).Capture() : null;
            return new BattleDefinitions(scenario.Variant, data.Fingerprint,
                data.Database.SkillOrder.Where(id => id > 0).Select(id => Skill(data.Database.SkillInfos[id])),
                data.Database.GameItemInfos.Values.Where(item => item.Id > 0).Select(Item),
                scenario.Participants.Select(actor => Actor(data, actor, scenario.Variant, cultivation)),
                data.Database.ComboSkillOrder.Select(id => Combo(data.Database.ComboSkillInfos[id])));
        }

        private static BattleComboDefinition Combo(ComboSkillInfo combo) => new(combo.Name,
            new BattleComboRequirement(checked((int)combo.Id), checked((int)combo.SkillId),
                combo.MainActorRequirements.Select(id => checked((int)id)),
                combo.ElementPositionRequirements.Select(position => (int)position),
                combo.WeaponTypeRequirements.Select(weapon => (int)weapon), combo.AuxiliaryStateConditions),
            (int)combo.TargetRangeType,
            Enumerable.Range(0, 12).Select(index => combo.AttributeImpacts.TryGetValue((ActorAttributeType)index, out AttributeImpact effect) ? (int)effect.Value : 0),
#if PAL3A
            combo.PowerMultiplierPercentage
#else
            0
#endif
        );

        private static BattleAbilityDefinition Skill(SkillInfo skill)
        {
            var reasons = new List<string>();
            int type = (int)skill.Type;
            bool fiveSpirit = BattleData.Variant == "PAL3A" && skill.Id is >= 264 and <= 268;
            bool pal3ASpecial = BattleData.Variant == "PAL3A" && BattlePal3ARules.IsDedicatedAbility(checked((int)skill.Id));
            BattleSpecialHandler handler = BattleSpecialRules.AbilityHandler(BattleData.Variant, checked((int)skill.Id), false, type is >= 1 and <= 3, skill.SpecialSkillId);
            if (type < 0 || type > 4)
                reasons.Add("Unknown original skill type " + type + ".");
            if (type == 4 && (BattleData.Variant != "PAL3" || !BattleMagicSwordRules.IsSupportedSkill(checked((int)skill.Id))))
                reasons.Add("Unknown original weapon skill.");
            if (skill.SpecialSkillId != 0 && handler == BattleSpecialHandler.None)
                reasons.Add("Original special skill handler " + skill.SpecialSkillId + " is not implemented.");
            int elements = Elements(skill.ElementAttributes);
            if ((elements & (elements - 1)) != 0 && type is not (1 or 2 or 3 or 4))
                reasons.Add("Composite elements are unverified for this skill type.");
            if (!BattleSpecialRules.SupportsSuccessRate(BattleData.Variant, checked((int)skill.Id), skill.SuccessRateLevel))
                reasons.Add("Original success formula " + skill.SuccessRateLevel + " is not implemented.");
            BattleAttributeEffect[] attributes = Attributes(skill.AttributeImpacts, reasons);
            BattleStatusEffect[] states = SkillStates(skill.CombatStateImpactTypes, reasons);
            if (attributes.Length == 0 && states.Length == 0 && skill.SpecialSkillId == 0 && handler != BattleSpecialHandler.GenericOnly && !fiveSpirit && !pal3ASpecial)
                reasons.Add("No verified generic effect is described by this original skill record.");
            var costs = new List<BattleCost>();
            if (skill.SpConsumeValue != 0)
                costs.Add(new BattleCost(1, (BattleValueMode)skill.SpConsumeImpactType, skill.SpConsumeValue));
            if (skill.MpConsumeValue != 0)
                costs.Add(new BattleCost(2, (BattleValueMode)skill.MpConsumeImpactType, skill.MpConsumeValue));
            if (skill.SpecialConsumeType != -1)
            {
                costs.Add(new BattleCost(skill.SpecialConsumeType, (BattleValueMode)skill.SpecialConsumeImpactType, skill.SpecialConsumeValue));
                if (skill.SpecialConsumeType is not (10 or 16) && !(skill.SpecialConsumeType == 0 && BattleSpecialRules.UsesAbsoluteHpCost(BattleData.Variant, checked((int)skill.Id))))
                    reasons.Add("Original special resource consumption requires a dedicated handler.");
            }
            var unlocks = new List<BattleSkillUnlock>();
            for (int index = 0; index < skill.CompositeSkillIds.Length; index++)
            {
                if (skill.CompositeSkillIds[index] == 0)
                    continue;
                unlocks.Add(new BattleSkillUnlock(checked((int)skill.CompositeSkillIds[index]), checked((int)skill.CompositeRequiredSkillIds[index]),
                    skill.CompositeRequiredSkillLevels[index], skill.CompositeRequiredCurrentSkillLevels[index], skill.CompositeRequiredActorLevels[index]));
            }
            return new BattleAbilityDefinition(new BattleAbilityData
            {
                Id = checked((int)skill.Id),
                Name = skill.Name,
                IsMagic = type is >= 1 and <= 3 || fiveSpirit,
                SkillTypeId = type,
                Range = Range(skill.TargetRangeType, reasons),
                ElementMask = elements,
                SuccessRate = skill.SuccessRateLevel,
                SpellRank = skill.Level,
                NextSkillRequiredProficiency = skill.NextSkillRequiredProficiency,
                LearningLevel = skill.LearningActorLevel,
                NextSkillId = checked((int)skill.NextSkillId),
                UsesBeforeLevelUp = skill.CumulativeUseThresholds.Select(value => (int)value).ToArray(),
                Attributes = attributes,
                States = states,
                Costs = costs.ToArray(),
                ApplicableIdentities = skill.ApplicableActors.Select(identity => (int)identity).ToArray(),
                SpecialEffect = skill.SpecialSkillId,
                Handler = handler,
                Unlocks = unlocks.ToArray(),
                UnsupportedReason = string.Join(" ", reasons)
            });
        }

        internal static BattleAbilityDefinition Item(GameItemInfo item)
        {
            var reasons = new List<string>();
            int type = (int)item.Type;
            BattleSpecialHandler handler = BattleSpecialRules.AbilityHandler(BattleData.Variant, checked((int)item.Id), true, false, (int)item.ItemSpecialType);
            if (type is not (1 or 2 or 3))
                reasons.Add("This original item category has no combat use command.");
            if ((int)item.ItemSpecialType != 0 && handler == BattleSpecialHandler.None)
                reasons.Add("Original item special handler " + (int)item.ItemSpecialType + " is not implemented.");
#if PAL3A
            if (type is 1 or 2 or 3 && item.UnresolvedValue2FC != 0)
                reasons.Add("The combat item uses an unresolved original value at +0x2fc.");
#endif
            return new BattleAbilityDefinition(new BattleAbilityData
            {
                Id = checked((int)item.Id),
                Name = item.Name,
                IsItem = true,
                IsTreasure = BattleSpecialRules.IsTreasure(BattleData.Variant, checked((int)item.Id)),
                ItemType = type,
                AiNullCureOperation = item.CombatStates.TryGetValue(ActorCombatStateType.ExpIncrease, out ItemCombatStateValue lastState) ? lastState.Flags : 0,
                BattleUsable = item.PlaceOfUseType is PlaceOfUseType.Combat or PlaceOfUseType.Anywhere,
                Range = Range(item.TargetRangeType, reasons),
                ElementMask = Elements(item.ElementAttributes),
                Attributes = Attributes(item.AttributeImpacts, reasons),
                States = ItemStates(item, reasons),
                ApplicableIdentities = item.ApplicableActors.Select(identity => (int)identity).ToArray(),
                SpecialEffect = (int)item.ItemSpecialType,
                Handler = handler,
                UnsupportedReason = string.Join(" ", reasons)
            });
        }

        internal static BattleActorDefinition Actor(BattleData data, BattleParticipant actor, string variant, BattleMagicSwordSnapshot cultivation = null)
        {
            CombatActorInfo info = data.Database.CombatActorInfos[actor.ActorId];
            bool main = info.Type == CombatActorType.MainActor;
            int identity = BattleData.PlayerIdentity(actor.ActorId);
            var reasons = new List<string>();
            var equipment = new List<BattleEquipmentDefinition>();
            for (int slot = 0; slot < actor.Equipment.Length; slot++)
            {
                int id = actor.Equipment[slot];
                if (id == 0)
                    continue;
                if (slot > 5 || !data.Database.GameItemInfos.TryGetValue(id, out GameItemInfo item))
                {
                    reasons.Add("Unknown equipment ID/slot " + id + "/" + slot + ".");
                    continue;
                }
                equipment.Add(Equipment(item, slot, identity, actor.MagicSword,
                    slot == 0 && BattleLoadoutRules.IsNativeDefaultWeapon(variant, actor), cultivation));
            }
            var permanent = new HashSet<BattleState>();
            var initialStates = new List<BattleState>();
            if (!main)
            {
                foreach (var state in info.CombatStateImpactTypes.OrderBy(state => (int)state.Key))
                {
                    if ((int)state.Value == 0)
                        continue;
                    if ((int)state.Key < 23)
                        initialStates.Add((BattleState)state.Key);
                    else if (state.Key == ActorCombatStateType.PoisonResist)
                        permanent.Add(BattleState.PoisonResist);
                }
            }
            if (!main && !string.IsNullOrEmpty(info.ModelId) && info.ModelId.StartsWith("b", StringComparison.OrdinalIgnoreCase))
                permanent.Add(BattleState.PoisonResist);
            foreach (BattleEquipmentDefinition item in equipment.Where(item => item.Slot != 0))
            {
                if (item.States.Any(state => state.State == BattleState.PoisonResist && state.Value != 0))
                    permanent.Add(BattleState.PoisonResist);
            }
            if (actor.MagicSword != null && (variant != "PAL3" || identity != 0 || actor.Side != BattleSide.Ally))
                reasons.Add("Only PAL3 Jingtian can own the captured magic sword.");
            if (main && identity < 0)
                reasons.Add("This main-character form needs its original transformation rules.");
            if (!main && info.SpecialActionId > 0)
                reasons.Add("Original actor special action " + info.SpecialActionId + " needs a dedicated handler.");
            bool moneyShield = variant == "PAL3" && actor.Equipment.Length >= 6 && actor.Equipment[1] == 80015 && actor.Equipment[2] == 84018 && actor.Equipment[3] == 83012 &&
                (actor.Equipment[4] == 8153 && actor.Equipment[5] == 8154 || actor.Equipment[4] == 8154 && actor.Equipment[5] == 8153);
            BattleTargetRange normalRange = BattleTargetRange.None;
            if (main)
            {
                BattleEquipmentDefinition weapon = equipment.FirstOrDefault(item => item.Slot == 0);
                if (weapon == null)
                    reasons.Add("A verified physical attack range requires an equipped weapon.");
                else
                {
                    try
                    {
                        normalRange = BattlePhysicalAttackModes.WeaponRange(variant, (int)data.Database.GameItemInfos[weapon.Id].WeaponType, identity);
                    }
                    catch (NotSupportedException exception)
                    {
                        reasons.Add(exception.Message);
                    }
                    catch (ArgumentOutOfRangeException exception)
                    {
                        reasons.Add(exception.Message);
                    }
                }
            }
            else
            {
                string unsupportedMode = BattlePhysicalAttackModes.UnsupportedReason(variant, checked((int)info.NormalAttackModeId));
                if (!string.IsNullOrEmpty(unsupportedMode))
                    reasons.Add(unsupportedMode);
                if (string.IsNullOrEmpty(unsupportedMode))
                    normalRange = BattlePhysicalAttackModes.Range(checked((int)info.NormalAttackModeId));
            }
            int attackElements = main ? equipment.Where(item => item.Slot == 0).Aggregate(0, (mask, item) => mask | item.ElementMask) : ActorElements(info);
            int defenseElements = main ? equipment.Where(item => item.Slot != 0).Aggregate(0, (mask, item) => mask | item.ElementMask) : ActorElements(info);
            var thresholds = new int[101];
            for (int level = 1; level < 100; level++)
                thresholds[level] = data.Layout.LevelExperienceTable[level];
            thresholds[100] = int.MaxValue;
            return new BattleActorDefinition(new BattleActorData
            {
                InstanceId = actor.InstanceId,
                Identity = identity,
                MagicSword = actor.MagicSword?.Capture(),
                Boss = info.ModelId?.StartsWith("b", StringComparison.OrdinalIgnoreCase) == true,
                CorpseItem = checked((int)info.CorpseId),
                CorpseSkill = checked((int)info.CorpseSkillId),
                CorpsePercent = info.CorpseSuccessRate,
                Equipment = equipment.ToArray(),
                PermanentStates = permanent.ToArray(),
                InitialStates = initialStates.ToArray(),
                PhysicalAttackElements = attackElements,
                PhysicalDefenseElements = defenseElements,
                PhysicalAttackElementValues = ElementValues(info, equipment, main, true),
                PhysicalDefenseElementValues = ElementValues(info, equipment, main, false),
                NormalAttackMode = checked((int)info.NormalAttackModeId),
                NormalAttackRange = normalRange,
                ExperienceReward = info.Experience,
                MoneyReward = info.MoneyWhenKilled,
                LootItem = checked((int)info.NormalLoot),
                LootCount = info.NormalLootCount,
                EscapePercent = (int)info.EscapeRate,
                StealableMoney = Math.Max(0, (int)info.StealableMoneyAmount),
                StealableItemId = checked((int)info.StealableItemId),
                StealableItemCount = Math.Max(0, (int)info.StealableItemCount),
                MoneyShield = moneyShield,
                ExperienceThresholds = main ? thresholds : Array.Empty<int>(),
                AttributesByLevel = main && identity >= 0 ? Growth(info, identity, variant) : Array.Empty<int[]>(),
                GrowthUnsupportedReason = main && identity >= 0 ? "" : "Original player level growth is unavailable for this actor form.",
                UnsupportedReason = string.Join(" ", reasons)
            });
        }

        private static BattleEquipmentDefinition Equipment(GameItemInfo item, int slot, int identity, BattleMagicSword magicSword,
            bool nativeDefault = false, BattleMagicSwordSnapshot cultivation = null)
        {
            var reasons = new List<string>();
            int expected = slot switch
            {
                0 => 11,
                1 => 8,
                2 => 7,
                3 => 9,
                _ => 10
            };
            if ((int)item.Type != expected)
                reasons.Add("The original item type does not match this equipment slot.");
            if (identity < 0 || !nativeDefault && !item.ApplicableActors.Contains((PlayerActorId)identity))
                reasons.Add("This character cannot equip the original item.");
            int special = (int)item.ItemSpecialType;
            if (!BattleSpecialRules.SupportsEquipmentSpecial(BattleData.Variant, checked((int)item.Id), slot, special))
                reasons.Add("Original equipment special handler " + special + " is not implemented.");
            if (item.Id == BattleMagicSwordRules.WeaponId && slot == 0 &&
                (BattleData.Variant != "PAL3" || identity == 0 && magicSword == null || identity != 0 && !nativeDefault))
                reasons.Add("Weapon 6603 needs PAL3 Jingtian and its original cultivation snapshot.");
            BattleStatusEffect[] states = slot == 0 ? ItemStates(item, reasons) : PassiveEquipmentStates(item);
            var recovery = new int[3];
            if (slot != 0)
            {
                for (int attribute = 0; attribute < 3; attribute++)
                    recovery[attribute] = item.AttributeImpacts.TryGetValue((ActorAttributeType)attribute, out AttributeImpact effect) ? effect.Value : 0;
            }
            return new BattleEquipmentDefinition(new BattleEquipmentData
            {
                Id = checked((int)item.Id),
                Slot = slot,
                WeaponType = slot == 0 ? (int)item.WeaponType : 0,
                MagicSwordCultivation = nativeDefault && item.Id == BattleMagicSwordRules.WeaponId && identity != 0
                    ? cultivation ?? new BattleMagicSword().Capture() : null,
                StartupInvisibility = BattleData.Variant == "PAL3" && slot >= 4 && item.Id == 8114,
                AttackStealPercent = slot != 0 && special == 23 ? 5 : 0,
                Name = item.Name,
                Attributes = Attributes(item.AttributeImpacts, reasons),
                States = states,
                ElementMask = Elements(item.ElementAttributes),
                SpSaving = slot == 0 ? 0 : item.SpSavingPercentage,
                MpSaving = slot == 0 ? 0 : item.MpSavingPercentage,
                CriticalBonus = slot == 0 ? 0 : item.CriticalAttackAmplifyPercentage,
                PhysicalReflectionPassive = slot >= 4 && item.Id == (BattleData.Variant == "PAL3" ? 8110 : 8108),
                TechniqueSuccessBonus = slot == 0 ? 0 : item.SpecialSkillSuccessRate,
                SpecialEffect = special,
                RecoveryPerRound = recovery,
                UnsupportedReason = string.Join(" ", reasons)
            });
        }

        private static BattleAttributeEffect[] Attributes(Dictionary<ActorAttributeType, AttributeImpact> source, List<string> reasons)
        {
            var result = new List<BattleAttributeEffect>();
            foreach (var pair in source.OrderBy(pair => (int)pair.Key))
            {
                int mode = (int)pair.Value.Type;
                if (mode < 0 || mode > 3)
                    reasons.Add("Unknown attribute mode " + mode + ".");
                else
                    result.Add(new BattleAttributeEffect((int)pair.Key, (BattleValueMode)mode, pair.Value.Value));
            }
            return result.ToArray();
        }

        private static BattleStatusEffect[] SkillStates(Dictionary<ActorCombatStateType, CombatStateImpactType> source, List<string> reasons)
        {
            var result = new List<BattleStatusEffect>();
            foreach (var pair in source.OrderBy(pair => (int)pair.Key))
            {
                int value = (int)pair.Value;
                if (value is 1 or 2)
                    result.Add(new BattleStatusEffect((BattleState)pair.Key, value == 2, value));
                else if (value != 0)
                    reasons.Add("Unknown original state operation " + value + ".");
            }
            return result.ToArray();
        }

        private static BattleStatusEffect[] PassiveEquipmentStates(GameItemInfo item)
        {
            // Both released nonweapon initializers read only the poison-resist
            // signed word at +0x2ee. Any nonzero value grants the permanent state;
            // its Flags byte and all other state fields are ignored here.
            return item.CombatStates.TryGetValue(ActorCombatStateType.PoisonResist, out ItemCombatStateValue resistance) && resistance.Value != 0
                ? new[] { new BattleStatusEffect(BattleState.PoisonResist, false, resistance.Value) }
                : Array.Empty<BattleStatusEffect>();
        }

        private static BattleStatusEffect[] ItemStates(GameItemInfo item, List<string> reasons)
        {
            var result = new List<BattleStatusEffect>();
            foreach (var pair in item.CombatStates.OrderBy(pair => (int)pair.Key))
            {
                int value = pair.Value.Value;
                if (item.Type == ItemType.Weapon)
                {
                    // For weapons the byte is an enable flag, the short is a
                    // probability; the native handler always adds the first hit.
                    if (pair.Value.Flags != 0)
                        result.Add(new BattleStatusEffect((BattleState)pair.Key, false, value));
                }
                else if (value is 1 or 2)
                    result.Add(new BattleStatusEffect((BattleState)pair.Key, value == 2, value));
                else if (value != 0)
                    reasons.Add("Unknown original item state value " + value + ".");
            }
            return result.ToArray();
        }

        private static BattleTargetRange Range(TargetRangeType range, List<string> reasons)
        {
            int value = (int)range;
            if (value < 0 || value > 6)
            {
                reasons.Add("Unknown original target range " + value + ".");
                return BattleTargetRange.None;
            }
            return (BattleTargetRange)value;
        }

        private static int Elements(IEnumerable<ObjectElementType> source) => source.Aggregate(0, (mask, element) => mask | (1 << (int)element));

        private static int ActorElements(CombatActorInfo info) => info.ElementAttributeValues.Aggregate(0, (mask, pair) => pair.Value == 0 ? mask : mask | (1 << ((int)pair.Key - 1)));

        private static int[] ElementValues(CombatActorInfo info, IEnumerable<BattleEquipmentDefinition> equipment, bool main, bool attack)
        {
            if (!main)
                return Enumerable.Range(1, 5).Select(index => info.ElementAttributeValues[(ElementType)index]).ToArray();
            var result = new int[5];
            foreach (BattleEquipmentDefinition item in equipment.Where(item => attack ? item.Slot == 0 : item.Slot != 0))
            {
                for (int element = 0; element < 5; element++)
                {
                    if ((item.ElementMask & (1 << element)) != 0)
                        result[element]++;
                }
            }
            return result;
        }

        private static int[][] Growth(CombatActorInfo info, int identity, string variant)
        {
            var result = new int[101][];
            result[0] = new int[12];
            for (int level = 1; level <= 100; level++)
            {
                int[] values = Enumerable.Range(0, 12).Select(index => info.AttributeValues[(ActorAttributeType)index]).ToArray();
                if (variant == "PAL3")
                {
                    values[0] = level * new[] { 8, 7, 9, 7, 9 }[identity] + new[] { 80, 75, 80, 60, 100 }[identity];
                    values[1] = BattleBasicRules.Round((float)(Math.Sqrt(level) * 9 + new[] { 8, 6, 7, 5, 9 }[identity]));
                    values[2] = level * new[] { 4, 5, 4, 6, 4 }[identity] + new[] { 25, 25, 22, 30, 20 }[identity];
                    values[3] = BattleBasicRules.Round(level * new[] { 4.5f, 4f, 4.5f, 4f, 5f }[identity] + new[] { 26, 24, 20, 22, 35 }[identity]);
                    values[4] = BattleBasicRules.Round(level * new[] { 3.3f, 3f, 3.5f, 3f, 3f }[identity] + new[] { 17, 13, 16, 16, 18 }[identity]);
                }
                else
                {
                    values[0] = level * new[] { 8, 9, 7, 9, 9 }[identity] + new[] { 70, 80, 65, 85, 90 }[identity];
                    values[1] = level + new[] { 8, 12, 6, 16, 10 }[identity];
                    values[2] = level * new[] { 4, 3, 8, 6, 5 }[identity] + new[] { 20, 16, 40, 30, 24 }[identity];
                    values[3] = BattleBasicRules.Round(level * new[] { 5f, 5.5f, 4.5f, 6f, 6f }[identity] + new[] { 20, 25, 18, 30, 28 }[identity]);
                    values[4] = BattleBasicRules.Round(level * new[] { 4f, 4f, 4f, 5f, 5f }[identity] + new[] { 18, 14, 16, 17, 20 }[identity]);
                }
                result[level] = values;
            }
            return result;
        }
    }
}
