// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Core.DataReader.Gdb
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Contracts;

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
            var combatActorInfos = new Dictionary<int, CombatActorInfo>();
            for (var i = 0; i < numOfActors; i++)
            {
                CombatActorInfo actor = ReadCombatActorInfo(reader, codepage);
                combatActorInfos[(int) actor.Id] = actor;
            }

            reader.Seek(skillDataOffset, SeekOrigin.Begin);
            var skillInfos = new Dictionary<int, SkillInfo>();
            for (var i = 0; i < numOfSkills; i++)
            {
                SkillInfo skill = ReadSkillInfo(reader, codepage);
                skillInfos[(int) skill.Id] = skill;
            }

            reader.Seek(itemDataOffset, SeekOrigin.Begin);
            var gameItems = new Dictionary<int, GameItemInfo>();
            for (var i = 0; i < numOfItems; i++)
            {
                GameItemInfo item = ReadGameItemInfo(reader, codepage);
                gameItems[(int) item.Id] = item;
            }

            reader.Seek(comboSkillDataOffset, SeekOrigin.Begin);
            var comboSkillInfos = new Dictionary<int, ComboSkillInfo>();
            for (var i = 0; i < numOfComboSkills; i++)
            {
                ComboSkillInfo comboSkill = ReadComboSkillInfo(reader, codepage);
                comboSkillInfos[(int) comboSkill.Id] = comboSkill;
            }

            return new GdbFile(combatActorInfos,
                skillInfos,
                gameItems,
                comboSkillInfos);
        }

        private static CombatActorInfo ReadCombatActorInfo(IBinaryReader reader, int codepage)
        {
            var id = reader.ReadUInt32();
            var type = (CombatActorType) reader.ReadUInt16();
            var description = reader.ReadString(512, codepage);
            var modelId = reader.ReadString(30, codepage);
            var iconId = reader.ReadString(32, codepage);
            var elementAttributeValues = reader.ReadInt32s(5);
            var name = reader.ReadString(32, codepage);
            var level = reader.ReadInt32();
            var attributeValues = reader.ReadInt32s(12);
            var combatStateImpactTypes = reader.ReadBytes(31);
            _ = reader.ReadByte(); // padding
            var maxRound = reader.ReadInt32();
            var specialActionId = reader.ReadInt32();
            var escapeRate = reader.ReadSingle();
            var mainActorFavor = reader.ReadUInt16s(6);
            var experience = reader.ReadInt32();
            var money = reader.ReadUInt16();
            _ = reader.ReadUInt16(); // padding
            var normalAttackModeId = reader.ReadUInt32();
            var heightLevel = reader.ReadByte();
            var moveRangeLevel = reader.ReadByte();
            var attackRangeLevel = reader.ReadByte();
            var moveSpeedLevel = reader.ReadByte();
            var chaseSpeed = reader.ReadByte();
            _ = reader.ReadBytes(3); // padding
            var skillIds = reader.ReadUInt32s(4);
            var skillLevels = reader.ReadBytes(4);
            var spImpactValue = reader.ReadInt32();
            var properties = reader.ReadSingles(10);
            var normalLoot = reader.ReadUInt32();
            var normalLootCount = reader.ReadInt16();
            _ = reader.ReadInt16(); // padding
            var corpseId = reader.ReadUInt32();
            var corpseSkillId = reader.ReadUInt32();
            var corpseSuccessRate = reader.ReadInt16();
            var stealableMoneyAmount = reader.ReadInt16();
            var stealableItemId = reader.ReadUInt32();
            var stealableItemCount = reader.ReadInt16();
            var moneyWhenKilled = reader.ReadInt16();

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
                CombatStateImpactTypes = GetCombatStateImpactTypes(combatStateImpactTypes),
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
            var id = reader.ReadUInt32();
            var type = (SkillType)reader.ReadByte();
            _ = reader.ReadBytes(3); // padding
            var elementAttributes = reader.ReadInt32s(5).Select(_ => (byte)_).ToArray();

            var name = reader.ReadString(32, codepage);
            var description = reader.ReadString(512, codepage);
            var mainActorCanUse = reader.ReadBytes(5);
            var targetRangeType = (TargetRangeType)reader.ReadByte();
            var specialSkillId = reader.ReadByte();
            var attributeImpactType = reader.ReadBytes(12);
            _ = reader.ReadByte(); // padding
            var attributeImpactValue = reader.ReadInt16s(12);
            var successRateLevel = reader.ReadByte();
            _ = reader.ReadByte(); // padding
            var combatStateImpactTypes = reader.ReadInt16s(31).Select(_ => (byte)_).ToArray();

            var spConsumeImpactType = (AttributeImpactType)reader.ReadInt32();
            var mpConsumeImpactType = (AttributeImpactType)reader.ReadInt32();

            var spConsumeValue = reader.ReadInt32();
            var mpConsumeValue = reader.ReadInt32();

            var specialConsumeType = reader.ReadInt32();
            var specialConsumeImpactType = (AttributeImpactType)reader.ReadByte();
            _ = reader.ReadBytes(3); // padding
            var specialConsumeValue = reader.ReadInt32();

            var level = reader.ReadByte();
            var timesBeforeLevelUp = reader.ReadBytes(4);

            var requiredActorLevel = reader.ReadByte();
            var magicLevel = reader.ReadByte();
            _ = reader.ReadByte(); // padding
            var nextLevelSkillId = reader.ReadUInt32();
            var isUsableOutsideCombat = reader.ReadByte();
            _ = reader.ReadBytes(3); // padding
            var compositeSkillIds = reader.ReadUInt32s(3);
            var compositeRequiredSkillIds = reader.ReadUInt32s(3);
            var compositeRequiredSkillLevels = reader.ReadBytes(3);
            var compositeRequiredCurrentSkillLevels = reader.ReadBytes(3);
            var compositeRequiredActorLevels = reader.ReadBytes(3);
            var canTriggerComboSkill = reader.ReadByte();
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
                CombatStateImpactTypes = GetCombatStateImpactTypes(combatStateImpactTypes),
                SpConsumeImpactType = spConsumeImpactType,
                MpConsumeImpactType = mpConsumeImpactType,
                SpConsumeValue = spConsumeValue,
                MpConsumeValue = mpConsumeValue,
                SpecialConsumeType = specialConsumeType,
                SpecialConsumeImpactType = specialConsumeImpactType,
                SpecialConsumeValue = specialConsumeValue,
                Level = level,
                TimesBeforeLevelUp = timesBeforeLevelUp,
                RequiredActorLevel = requiredActorLevel,
                MagicLevel = magicLevel,
                NextLevelSkillId = nextLevelSkillId,
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
            var id = reader.ReadUInt32();
            var name = reader.ReadString(32, codepage);
            var modelName = reader.ReadString(30, codepage);
            var iconName = reader.ReadString(30, codepage);
            var description = reader.ReadString(512, codepage);
            var price = reader.ReadInt32();
            var type = (ItemType) reader.ReadByte();
            var weaponType = (WeaponType) reader.ReadByte();
            var mainActorCanUse = reader.ReadBytes(5);
            var elementAttributes = reader.ReadBytes(5);
            var ancientValue = reader.ReadInt32();
            var itemSpecialType = (ItemSpecialType) reader.ReadByte();
            var targetRangeType = (TargetRangeType) reader.ReadByte();
            var placeOfUseType = (PlaceOfUseType) reader.ReadByte();
            var attributeImpactType = reader.ReadBytes(12);
            _ = reader.ReadByte(); // padding
            var attributeImpactValue = reader.ReadInt16s(12);
            var combatStateImpactTypes = reader.ReadBytes(31);
            _ = reader.ReadByte(); // padding
            var combatStateImpactValue = reader.ReadInt16s(31);
            _ = reader.ReadBytes(2); // padding

            #if PAL3
            var comboCount = reader.ReadInt32();
            var spSavingPercentage = reader.ReadInt16();
            var mpSavingPercentage = reader.ReadInt16();
            var criticalAttackAmplifyPercentage = reader.ReadInt16();
            var specialSkillSuccessRate = reader.ReadInt16();
            var oreId = reader.ReadUInt32();
            var productId = reader.ReadUInt32();
            var productPrice = reader.ReadInt32();
            var synthesisMaterialIds = reader.ReadUInt32s(2);
            var synthesisProductId = reader.ReadUInt32();
            #elif PAL3A
            var unknown1 = reader.ReadInt32();
            var unknown2 = reader.ReadUInt32();

            var spSavingPercentage = reader.ReadInt16();
            var mpSavingPercentage = reader.ReadInt16();
            var criticalAttackAmplifyPercentage = reader.ReadInt16();
            var specialSkillSuccessRate = reader.ReadInt16();

            var creatorActorId = (PlayerActorId)reader.ReadInt32();
            var materialId = reader.ReadUInt32();
            var productType = reader.ReadInt32();
            var productId = reader.ReadUInt32();
            var requiredFavorValue = reader.ReadUInt32();
            #endif

            return new GameItemInfo()
            {
                Id = id,
                Name = name,
                ModelName = modelName,
                IconName = iconName,
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
                AttributeImpacts = GetActorAttributeImpacts(attributeImpactType, attributeImpactValue),
                CombatStateImpacts = GetCombatStateImpacts(
                    GetCombatStateImpactTypes(combatStateImpactTypes), combatStateImpactValue),

                #if PAL3
                ComboCount = comboCount,
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
                Unknown1 = unknown1,
                Unknown2 = unknown2,
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
            var name = reader.ReadString(32, codepage);
            var id = reader.ReadUInt32();
            var mainActorRequirements = reader.ReadUInt32s(4);
            var elementPositionRequirements = reader.ReadBytes(4)
                .Select(_ => (ElementPositionRequirementType)_).ToArray();
            var skillId = reader.ReadUInt32();
            var weaponTypeRequirements = reader.ReadBytes(4).Select(_ => (WeaponType)_).ToArray();
            _ = reader.ReadBytes(4); // not used
            var combatStateRequirements = reader.ReadInt32s(3)
                .Select(_ => (ActorCombatStateType)_).ToArray();
            var description = reader.ReadString(512, codepage);
            var targetRangeType = (TargetRangeType)reader.ReadByte();

            var attributeImpactType = reader.ReadBytes(12);
            _ = reader.ReadBytes(3); // padding
            var attributeImpactValue = reader.ReadInt16s(12);

            #if PAL3A
            var unknown = reader.ReadInt32();
            #endif

            return new ComboSkillInfo()
            {
                Name = name,
                Id = id,
                MainActorRequirements = mainActorRequirements,
                ElementPositionRequirements = elementPositionRequirements,
                SkillId = skillId,
                WeaponTypeRequirements = weaponTypeRequirements,
                CombatStateRequirements = combatStateRequirements,
                Description = description,
                TargetRangeType = targetRangeType,
                AttributeImpacts = GetActorAttributeImpacts(attributeImpactType, attributeImpactValue),
                #if PAL3A
                Unknown = unknown,
                #endif
            };
        }

        private static Dictionary<ElementType, int> GetElementAttributeValues(int[] elementAttributeValues)
        {
            Dictionary<ElementType, int> elementAttributeValueMap = new ();

            foreach (ElementType elementType in Enum.GetValues(typeof(ElementType)))
            {
                if (elementType == ElementType.None) continue;
                elementAttributeValueMap[elementType] = elementAttributeValues[(int)elementType - 1];
            }

            return elementAttributeValueMap;
        }

        private static Dictionary<ActorAttributeType, int> GetActorAttributeValues(int[] attributeValues)
        {
            Dictionary<ActorAttributeType, int> attributeValueMap = new ();

            foreach (ActorAttributeType attributeType in Enum.GetValues(typeof(ActorAttributeType)))
            {
                attributeValueMap[attributeType] = attributeValues[(int)attributeType];
            }

            return attributeValueMap;
        }

        private static HashSet<PlayerActorId> GetPlayerActorIds(byte[] mainActorCanUse)
        {
            HashSet<PlayerActorId> playerActorIds = new ();

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
            HashSet<ObjectElementType> elementAttributeSet = new ();

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
            byte[] combatStateImpactTypes)
        {
            Dictionary<ActorCombatStateType, CombatStateImpactType> combatStateImpacts = new ();

            foreach (ActorCombatStateType combatState in Enum.GetValues(typeof(ActorCombatStateType)))
            {
                CombatStateImpactType impactType = (CombatStateImpactType)combatStateImpactTypes[(int) combatState];
                if (impactType != CombatStateImpactType.None)
                {
                    combatStateImpacts[combatState] = impactType;
                }
            }

            return combatStateImpacts;
        }

        private static Dictionary<ActorCombatStateType, CombatStateImpact> GetCombatStateImpacts(
            Dictionary<ActorCombatStateType, CombatStateImpactType> combatStateImpactTypes,
            short[] combatStateImpactValues)
        {
            Dictionary<ActorCombatStateType, CombatStateImpact> combatStateImpacts = new ();

            foreach (ActorCombatStateType combatState in Enum.GetValues(typeof(ActorCombatStateType)))
            {
                short impactValue = combatStateImpactValues[(int) combatState];
                if (impactValue != 0)
                {
                    combatStateImpacts[combatState] = new CombatStateImpact
                    {
                        Type = combatStateImpactTypes.TryGetValue(combatState, out CombatStateImpactType impactType) ?
                            impactType : CombatStateImpactType.None,
                        Value = impactValue
                    };
                }
            }

            return combatStateImpacts;
        }

        private static Dictionary<ActorAttributeType, AttributeImpact> GetActorAttributeImpacts(
            byte[] attributeImpactType, short[] attributeImpactValue)
        {
            Dictionary<ActorAttributeType, AttributeImpact> attributeImpacts = new ();

            foreach (ActorAttributeType attributeType in Enum.GetValues(typeof(ActorAttributeType)))
            {
                short impactValue = attributeImpactValue[(int) attributeType];
                if (impactValue != 0)
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