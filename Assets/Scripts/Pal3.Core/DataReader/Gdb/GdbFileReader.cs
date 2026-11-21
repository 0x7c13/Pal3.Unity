// ------------------------------------------------------------------------
//  Copyright (c) 2021-2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Core.DataReader.Gdb
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Contract.Enums;

    public sealed class GdbFileReader : IFileReader<GdbFile>
    {
        public GdbFile Read(IBinaryReader reader, int codepage)
        {
            _ = reader.ReadString(128, codepage); // header string

            uint combatActorDataOffset = reader.ReadUInt32();
            int numOfActors = reader.ReadInt32();

            uint skillDataOffset = reader.ReadUInt32();
            int numOfSkills = reader.ReadInt32();

            uint itemDataOffset = reader.ReadUInt32();
            int numOfItems = reader.ReadInt32();

            uint comboSkillDataOffset = reader.ReadUInt32();
            int numOfComboSkills = reader.ReadInt32();

            reader.Seek(combatActorDataOffset, SeekOrigin.Begin);
            Dictionary<int, CombatActorInfo> combatActorInfos = new();
            for (int i = 0; i < numOfActors; i++)
            {
                CombatActorInfo actor = ReadCombatActorInfo(reader, codepage);
                combatActorInfos[(int)actor.Id] = actor;
            }

            reader.Seek(skillDataOffset, SeekOrigin.Begin);
            Dictionary<int, SkillInfo> skillInfos = new();
            List<int> skillOrder = new();
            for (int i = 0; i < numOfSkills; i++)
            {
                SkillInfo skill = ReadSkillInfo(reader, codepage);
                skillInfos[(int)skill.Id] = skill;
                skillOrder.Add((int)skill.Id);
            }

            reader.Seek(itemDataOffset, SeekOrigin.Begin);
            Dictionary<int, GameItemInfo> gameItems = new();
            for (int i = 0; i < numOfItems; i++)
            {
                GameItemInfo item = ReadGameItemInfo(reader, codepage);
                gameItems[(int)item.Id] = item;
            }

            reader.Seek(comboSkillDataOffset, SeekOrigin.Begin);
            Dictionary<int, ComboSkillInfo> comboSkillInfos = new();
            List<int> comboSkillOrder = new();
            for (int i = 0; i < numOfComboSkills; i++)
            {
                ComboSkillInfo comboSkill = ReadComboSkillInfo(reader, codepage);
                comboSkillInfos[(int)comboSkill.Id] = comboSkill;
                comboSkillOrder.Add((int)comboSkill.Id);
            }

            return new GdbFile(combatActorInfos,
                skillInfos,
                gameItems,
                comboSkillInfos, comboSkillOrder, skillOrder);
        }

        private static CombatActorInfo ReadCombatActorInfo(IBinaryReader reader, int codepage)
        {
            uint id = reader.ReadUInt32();
            CombatActorType type = (CombatActorType)reader.ReadUInt16();
            string description = reader.ReadString(512, codepage);
            string modelId = reader.ReadString(30, codepage);
            string iconId = reader.ReadString(32, codepage);
            int[] elementAttributeValues = reader.ReadInt32s(5);
            string name = reader.ReadString(32, codepage);
            int level = reader.ReadInt32();
            int[] attributeValues = reader.ReadInt32s(12);
            byte[] combatStateImpactTypes = reader.ReadBytes(31);
            _ = reader.ReadByte(); // padding
            int maxRound = reader.ReadInt32();
            int specialActionId = reader.ReadInt32();
            float escapeRate = reader.ReadSingle();
            ushort[] mainActorFavor = reader.ReadUInt16s(6);
            int experience = reader.ReadInt32();
            ushort money = reader.ReadUInt16();
            _ = reader.ReadUInt16(); // padding
            uint normalAttackModeId = reader.ReadUInt32();
            byte heightLevel = reader.ReadByte();
            byte moveRangeLevel = reader.ReadByte();
            byte attackRangeLevel = reader.ReadByte();
            byte moveSpeedLevel = reader.ReadByte();
            byte chaseSpeed = reader.ReadByte();
            _ = reader.ReadBytes(3); // padding
            uint[] skillIds = reader.ReadUInt32s(4);
            byte[] skillLevels = reader.ReadBytes(3);
            _ = reader.ReadByte(); // padding; the fourth monster skill always has proficiency 4
            int spImpactValue = reader.ReadInt32();
            float[] properties = reader.ReadSingles(10);
            uint normalLoot = reader.ReadUInt32();
            short normalLootCount = reader.ReadInt16();
            _ = reader.ReadInt16(); // padding
            uint corpseId = reader.ReadUInt32();
            uint corpseSkillId = reader.ReadUInt32();
            short corpseSuccessRate = reader.ReadInt16();
            short stealableMoneyAmount = reader.ReadInt16();
            uint stealableItemId = reader.ReadUInt32();
            short stealableItemCount = reader.ReadInt16();
            short moneyWhenKilled = reader.ReadInt16();

            return new CombatActorInfo()
            {
                Id = id,
                Type = type,
                Description = description,
                ModelId = modelId,
                IconId = iconId,
                ElementAttributeValues = GetElementAttributeValues(elementAttributeValues),
                Name = name,
                Level = level,
                AttributeValues = GetActorAttributeValues(attributeValues),
                CombatStateImpactTypes = GetCombatStateImpactTypes(combatStateImpactTypes.Select(value => (int)value).ToArray()),
                MaxRound = maxRound,
                SpecialActionId = specialActionId,
                EscapeRate = escapeRate,
                MainActorFavor = mainActorFavor,
                Experience = experience,
                Money = money,
                NormalAttackModeId = normalAttackModeId,
                HeightLevel = heightLevel,
                MoveRangeLevel = moveRangeLevel,
                AttackRangeLevel = attackRangeLevel,
                MoveSpeedLevel = moveSpeedLevel,
                ChaseSpeedLevel = chaseSpeed,
                SkillIds = skillIds,
                SkillLevels = skillLevels,
                SpImpactValue = spImpactValue,
                Properties = properties,
                NormalLoot = normalLoot,
                NormalLootCount = normalLootCount,
                CorpseId = corpseId,
                CorpseSkillId = corpseSkillId,
                CorpseSuccessRate = corpseSuccessRate,
                StealableMoneyAmount = stealableMoneyAmount,
                StealableItemId = stealableItemId,
                StealableItemCount = stealableItemCount,
                MoneyWhenKilled = moneyWhenKilled,
            };
        }

        private SkillInfo ReadSkillInfo(IBinaryReader reader, int codepage)
        {
            uint id = reader.ReadUInt32();
            SkillType type = (SkillType)reader.ReadByte();
            _ = reader.ReadBytes(3); // padding
            byte[] elementAttributes = reader.ReadInt32s(5).Select(_ => (byte)_).ToArray();

            string name = reader.ReadString(32, codepage);
            string description = reader.ReadString(512, codepage);
            byte[] mainActorCanUse = reader.ReadBytes(5);
            TargetRangeType targetRangeType = (TargetRangeType)reader.ReadByte();
            byte specialSkillId = reader.ReadByte();
            byte[] attributeImpactType = reader.ReadBytes(12);
            _ = reader.ReadByte(); // padding
            short[] attributeImpactValue = reader.ReadInt16s(12);
            byte successRateLevel = reader.ReadByte();
            _ = reader.ReadByte(); // padding
            short[] combatStateImpactTypes = reader.ReadInt16s(31);

            AttributeImpactType spConsumeImpactType = (AttributeImpactType)reader.ReadInt32();
            AttributeImpactType mpConsumeImpactType = (AttributeImpactType)reader.ReadInt32();

            int spConsumeValue = reader.ReadInt32();
            int mpConsumeValue = reader.ReadInt32();

            int specialConsumeType = reader.ReadInt32();
            AttributeImpactType specialConsumeImpactType = (AttributeImpactType)reader.ReadByte();
            _ = reader.ReadBytes(3); // padding
            int specialConsumeValue = reader.ReadInt32();

            byte level = reader.ReadByte();
            byte[] timesBeforeLevelUp = reader.ReadBytes(4);

            byte requiredActorLevel = reader.ReadByte();
            byte magicLevel = reader.ReadByte();
            _ = reader.ReadByte(); // padding
            uint nextLevelSkillId = reader.ReadUInt32();
            byte isUsableOutsideCombat = reader.ReadByte();
            _ = reader.ReadBytes(3); // padding
            uint[] compositeSkillIds = reader.ReadUInt32s(3);
            uint[] compositeRequiredSkillIds = reader.ReadUInt32s(3);
            byte[] compositeRequiredSkillLevels = reader.ReadBytes(3);
            byte[] compositeRequiredCurrentSkillLevels = reader.ReadBytes(3);
            byte[] compositeRequiredActorLevels = reader.ReadBytes(3);
            byte canTriggerComboSkill = reader.ReadByte();
            _ = reader.ReadBytes(2); // padding

            return new SkillInfo()
            {
                Id = id,
                Type = type,
                ElementAttributes = GetElementAttributes(elementAttributes),
                Name = name,
                Description = description,
                ApplicableActors = GetPlayerActorIds(mainActorCanUse),
                TargetRangeType = targetRangeType,
                SpecialSkillId = specialSkillId,
                AttributeImpacts = GetActorAttributeImpacts(attributeImpactType, attributeImpactValue),
                SuccessRateLevel = successRateLevel,
                CombatStateImpactTypes = GetCombatStateImpactTypes(combatStateImpactTypes.Select(value => (int)value).ToArray()),
                SpConsumeImpactType = spConsumeImpactType,
                MpConsumeImpactType = mpConsumeImpactType,
                SpConsumeValue = spConsumeValue,
                MpConsumeValue = mpConsumeValue,
                SpecialConsumeType = specialConsumeType,
                SpecialConsumeImpactType = specialConsumeImpactType,
                SpecialConsumeValue = specialConsumeValue,
                Level = level,
                CumulativeUseThresholds = timesBeforeLevelUp,
                LearningActorLevel = requiredActorLevel,
                NextSkillRequiredProficiency = magicLevel,
                NextSkillId = nextLevelSkillId,
                IsUsableOutsideCombat = isUsableOutsideCombat,
                CompositeSkillIds = compositeSkillIds,
                CompositeRequiredSkillIds = compositeRequiredSkillIds,
                CompositeRequiredSkillLevels = compositeRequiredSkillLevels,
                CompositeRequiredCurrentSkillLevels = compositeRequiredCurrentSkillLevels,
                CompositeRequiredActorLevels = compositeRequiredActorLevels,
                CanTriggerComboSkill = canTriggerComboSkill,
            };
        }

        private static GameItemInfo ReadGameItemInfo(IBinaryReader reader, int codepage)
        {
            uint id = reader.ReadUInt32();
            string name = reader.ReadString(32, codepage);
            string modelName = reader.ReadString(30, codepage);
            string imageId = reader.ReadString(30, codepage);
            string description = reader.ReadString(512, codepage);
            int price = reader.ReadInt32();
            ItemType type = (ItemType)reader.ReadByte();
            WeaponType weaponType = (WeaponType)reader.ReadByte();
            byte[] mainActorCanUse = reader.ReadBytes(5);
            byte[] elementAttributes = reader.ReadBytes(5);
            int ancientValue = reader.ReadInt32();
            ItemSpecialType itemSpecialType = (ItemSpecialType)reader.ReadByte();
            TargetRangeType targetRangeType = (TargetRangeType)reader.ReadByte();
            PlaceOfUseType placeOfUseType = (PlaceOfUseType)reader.ReadByte();
            byte[] attributeImpactType = reader.ReadBytes(12);
            _ = reader.ReadByte(); // padding
            short[] attributeImpactValue = reader.ReadInt16s(12);
            byte[] combatStateImpactTypes = reader.ReadBytes(31);
            _ = reader.ReadByte(); // padding
            short[] combatStateImpactValue = reader.ReadInt16s(31);
            _ = reader.ReadBytes(2); // padding

#if PAL3
            int attackHitCount = reader.ReadInt32();
            short spSavingPercentage = reader.ReadInt16();
            short mpSavingPercentage = reader.ReadInt16();
            short criticalAttackAmplifyPercentage = reader.ReadInt16();
            short specialSkillSuccessRate = reader.ReadInt16();
            uint oreId = reader.ReadUInt32();
            uint productId = reader.ReadUInt32();
            int productPrice = reader.ReadInt32();
            uint[] synthesisMaterialIds = reader.ReadUInt32s(2);
            uint synthesisProductId = reader.ReadUInt32();
#elif PAL3A
            int unresolvedValue2FC = reader.ReadInt32();
            int attackHitCount = reader.ReadInt32();

            short spSavingPercentage = reader.ReadInt16();
            short mpSavingPercentage = reader.ReadInt16();
            short criticalAttackAmplifyPercentage = reader.ReadInt16();
            short specialSkillSuccessRate = reader.ReadInt16();

            PlayerActorId creatorActorId = (PlayerActorId)reader.ReadInt32();
            uint materialId = reader.ReadUInt32();
            int productType = reader.ReadInt32();
            uint productId = reader.ReadUInt32();
            uint requiredFavorValue = reader.ReadUInt32();
#endif

            return new GameItemInfo()
            {
                Id = id,
                Name = name,
                ModelName = modelName,
                ImageId = imageId,
                Description = description,
                Price = price,
                Type = type,
                WeaponType = weaponType,
                ApplicableActors = GetPlayerActorIds(mainActorCanUse),
                ElementAttributes = GetElementAttributes(elementAttributes),
                AncientValue = ancientValue,
                ItemSpecialType = itemSpecialType,
                TargetRangeType = targetRangeType,
                PlaceOfUseType = placeOfUseType,
                AttributeImpacts = GetActorAttributeImpacts(attributeImpactType, attributeImpactValue, includeZero: true),
                CombatStates = GetItemCombatStates(combatStateImpactTypes, combatStateImpactValue),
                AttackHitCount = attackHitCount,

#if PAL3
                SpSavingPercentage = spSavingPercentage,
                MpSavingPercentage = mpSavingPercentage,
                CriticalAttackAmplifyPercentage = criticalAttackAmplifyPercentage,
                SpecialSkillSuccessRate = specialSkillSuccessRate,
                OreId = oreId,
                ProductId = productId,
                ProductPrice = productPrice,
                SynthesisMaterialIds = synthesisMaterialIds,
                SynthesisProductId = synthesisProductId,
#elif PAL3A
                UnresolvedValue2FC = unresolvedValue2FC,
                SpSavingPercentage = spSavingPercentage,
                MpSavingPercentage = mpSavingPercentage,
                CriticalAttackAmplifyPercentage = criticalAttackAmplifyPercentage,
                SpecialSkillSuccessRate = specialSkillSuccessRate,
                CreatorActorId = creatorActorId,
                MaterialId = materialId,
                ProductType = productType,
                ProductId = productId,
                RequiredFavorValue = requiredFavorValue,
#endif
            };
        }

        private ComboSkillInfo ReadComboSkillInfo(IBinaryReader reader, int codepage)
        {
            string name = reader.ReadString(32, codepage);
            uint id = reader.ReadUInt32();
            uint[] mainActorRequirements = reader.ReadUInt32s(4);
            ElementPositionRequirementType[] elementPositionRequirements = reader.ReadBytes(4)
                .Select(_ => (ElementPositionRequirementType)_).ToArray();
            uint skillId = reader.ReadUInt32();
            WeaponType[] weaponTypeRequirements = reader.ReadBytes(4).Select(_ => (WeaponType)_).ToArray();
            _ = reader.ReadBytes(4); // not used
            int[] auxiliaryStateConditions = reader.ReadInt32s(3);
            string description = reader.ReadString(512, codepage);
            ComboTargetRangeType targetRangeType = (ComboTargetRangeType)reader.ReadByte();

            byte[] attributeImpactType = reader.ReadBytes(12);
            _ = reader.ReadByte(); // align the signed short array at +0x25e
            short[] attributeImpactValue = reader.ReadInt16s(12);
            _ = reader.ReadBytes(2); // align the record tail at +0x278

#if PAL3A
            int powerMultiplierPercentage = reader.ReadInt32();
#endif

            return new ComboSkillInfo()
            {
                Name = name,
                Id = id,
                MainActorRequirements = mainActorRequirements,
                ElementPositionRequirements = elementPositionRequirements,
                SkillId = skillId,
                WeaponTypeRequirements = weaponTypeRequirements,
                AuxiliaryStateConditions = auxiliaryStateConditions,
                Description = description,
                TargetRangeType = targetRangeType,
                AttributeImpacts = GetActorAttributeImpacts(attributeImpactType, attributeImpactValue, includeZero: true),
#if PAL3A
                PowerMultiplierPercentage = powerMultiplierPercentage,
#endif
            };
        }

        private static Dictionary<ElementType, int> GetElementAttributeValues(int[] elementAttributeValues)
        {
            Dictionary<ElementType, int> elementAttributeValueMap = new();

            foreach (ElementType elementType in Enum.GetValues(typeof(ElementType)))
            {
                if (elementType == ElementType.None)
                    continue;
                elementAttributeValueMap[elementType] = elementAttributeValues[(int)elementType - 1];
            }

            return elementAttributeValueMap;
        }

        private static Dictionary<ActorAttributeType, int> GetActorAttributeValues(int[] attributeValues)
        {
            Dictionary<ActorAttributeType, int> attributeValueMap = new();

            foreach (ActorAttributeType attributeType in Enum.GetValues(typeof(ActorAttributeType)))
            {
                attributeValueMap[attributeType] = attributeValues[(int)attributeType];
            }

            return attributeValueMap;
        }

        private static HashSet<PlayerActorId> GetPlayerActorIds(byte[] mainActorCanUse)
        {
            HashSet<PlayerActorId> playerActorIds = new();

            for (int i = 0; i < 5; i++)
            {
                if (mainActorCanUse[i] == 1)
                {
                    playerActorIds.Add((PlayerActorId)i);
                }
            }

            return playerActorIds;
        }

        private static HashSet<ObjectElementType> GetElementAttributes(byte[] elementAttributes)
        {
            HashSet<ObjectElementType> elementAttributeSet = new();

            for (int i = 0; i < 5; i++)
            {
                if (elementAttributes[i] == 1)
                {
                    elementAttributeSet.Add((ObjectElementType)i);
                }
            }

            return elementAttributeSet;
        }

        private static Dictionary<ActorCombatStateType, CombatStateImpactType> GetCombatStateImpactTypes(
            int[] combatStateImpactTypes)
        {
            Dictionary<ActorCombatStateType, CombatStateImpactType> combatStateImpacts = new();

            foreach (ActorCombatStateType combatState in Enum.GetValues(typeof(ActorCombatStateType)))
            {
                CombatStateImpactType impactType = (CombatStateImpactType)combatStateImpactTypes[(int)combatState];
                if (impactType != CombatStateImpactType.None)
                {
                    combatStateImpacts[combatState] = impactType;
                }
            }

            return combatStateImpacts;
        }

        private static Dictionary<ActorCombatStateType, ItemCombatStateValue> GetItemCombatStates(
            byte[] combatStateFlags,
            short[] combatStateImpactValues)
        {
            Dictionary<ActorCombatStateType, ItemCombatStateValue> combatStateImpacts = new();

            foreach (ActorCombatStateType combatState in Enum.GetValues(typeof(ActorCombatStateType)))
            {
                short impactValue = combatStateImpactValues[(int)combatState];
                if (impactValue != 0 || combatStateFlags[(int)combatState] != 0)
                {
                    combatStateImpacts[combatState] = new ItemCombatStateValue
                    {
                        Flags = combatStateFlags[(int)combatState],
                        Value = impactValue
                    };
                }
            }

            return combatStateImpacts;
        }

        private static Dictionary<ActorAttributeType, AttributeImpact> GetActorAttributeImpacts(
            byte[] attributeImpactType, short[] attributeImpactValue, bool includeZero = false)
        {
            Dictionary<ActorAttributeType, AttributeImpact> attributeImpacts = new();

            foreach (ActorAttributeType attributeType in Enum.GetValues(typeof(ActorAttributeType)))
            {
                short impactValue = attributeImpactValue[(int)attributeType];
                if (includeZero || impactValue != 0)
                {
                    attributeImpacts[attributeType] = new AttributeImpact()
                    {
                        Type = (AttributeImpactType)attributeImpactType[(int)attributeType],
                        Value = impactValue,
                    };
                }
            }

            return attributeImpacts;
        }
    }
}
