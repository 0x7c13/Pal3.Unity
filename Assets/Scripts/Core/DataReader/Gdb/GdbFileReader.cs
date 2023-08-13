// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Core.DataReader.Gdb
{
    using System.Collections.Generic;
    using System.IO;

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
            var wuLing = reader.ReadInt32s(5);
            var name = reader.ReadString(32, codepage);
            var level = reader.ReadInt32();
            var attributeValue = reader.ReadInt32s(12);
            var combatStateImpactType = reader.ReadBytes(31);
            _ = reader.ReadByte(); // padding
            var roundNumber = reader.ReadInt32();
            var specialActionId = reader.ReadInt32();
            var escapeRate = reader.ReadSingle();
            var mainActorFavor = reader.ReadUInt16s(6);
            var experience = reader.ReadInt32();
            var money = reader.ReadUInt16();
            _ = reader.ReadUInt16(); // padding
            var normalAttackActionId = reader.ReadUInt32();
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
                WuLing = wuLing,
                Name = name,
                Level = level,
                AttributeValue = attributeValue,
                CombatStateImpactType = combatStateImpactType,
                RoundNumber = roundNumber,
                SpecialActionId = specialActionId,
                EscapeRate = escapeRate,
                MainActorFavor = mainActorFavor,
                Experience = experience,
                Money = money,
                NormalAttackActionId = normalAttackActionId,
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
            var wuLing = reader.ReadInt32s(5);
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
            var combatStateImpactType = reader.ReadInt16s(31);
            var consumeAttributeType = reader.ReadInt32s(2);
            var consumeAttributeKind = reader.ReadInt32s(3);
            var specialConsumeType = reader.ReadByte();
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
                WuLing = wuLing,
                Name = name,
                Description = description,
                MainActorCanUse = mainActorCanUse,
                TargetRangeType = targetRangeType,
                SpecialSkillId = specialSkillId,
                AttributeImpactType = attributeImpactType,
                AttributeImpactValue = attributeImpactValue,
                SuccessRateLevel = successRateLevel,
                CombatStateImpactType = combatStateImpactType,
                ConsumeAttributeType = consumeAttributeType,
                ConsumeAttributeKind = consumeAttributeKind,
                SpecialConsumeType = specialConsumeType,
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
            var wuLing = reader.ReadBytes(5);
            var ancientValue = reader.ReadInt32();
            var specialType = (SpecialType) reader.ReadByte();
            var targetRangeType = (TargetRangeType) reader.ReadByte();
            var placeOfUseType = (PlaceOfUseType) reader.ReadByte();
            var attributeImpactType = reader.ReadBytes(12);
            _ = reader.ReadByte(); // padding
            var attributeImpactValue = reader.ReadInt16s(12);
            var combatStateImpactType = reader.ReadBytes(31);
            _ = reader.ReadByte(); // padding
            var combatStateImpactValue = reader.ReadInt16s(31);
            _ = reader.ReadBytes(2); // padding
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
                MainActorCanUse = mainActorCanUse,
                WuLing = wuLing,
                AncientValue = ancientValue,
                SpecialType = specialType,
                TargetRangeType = targetRangeType,
                PlaceOfUseType = placeOfUseType,
                AttributeImpactType = attributeImpactType,
                AttributeImpactValue = attributeImpactValue,
                CombatStateImpactType = combatStateImpactType,
                CombatStateImpactValue = combatStateImpactValue,
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
            };
        }

        private ComboSkillInfo ReadComboSkillInfo(IBinaryReader reader, int codepage)
        {
            var name = reader.ReadString(32, codepage);
            var id = reader.ReadUInt32();
            var mainActorRequirements = reader.ReadUInt32s(4);
            var wuLingPositionRequirements = reader.ReadBytes(4);
            var skillId = reader.ReadUInt32();
            var weaponTypeRequirements = reader.ReadBytes(4);
            _ = reader.ReadBytes(4); // not used
            var combatStateRequirements = reader.ReadInt32s(3);
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
                WuLingPositionRequirements = wuLingPositionRequirements,
                SkillId = skillId,
                WeaponTypeRequirements = weaponTypeRequirements,
                CombatStateRequirements = combatStateRequirements,
                Description = description,
                TargetRangeType = targetRangeType,
                AttributeImpactType = attributeImpactType,
                AttributeImpactValue = attributeImpactValue,
                #if PAL3A
                Unknown = unknown,
                #endif
            };
        }
    }
}