// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2025, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Core.DataReader.Gdb
{
    using System.Collections.Generic;
    using Contract.Enums;

    public struct AttributeImpact
    {
        public AttributeImpactType Type;     // 属性影响类型
        public short Value;                  // 属性影响值
    }

    public struct CombatStateImpact
    {
        public CombatStateImpactType Type;   // 战斗状态影响类型
        public short Value;                  // 战斗状态影响值
    }

    public struct GameItemInfo
    {
        public uint Id;                             // 物品ID

        public string Name;                         // 物品名称
        public string ModelName;                    // 物品模型名称
        public string IconName;                     // 物品图标名称
        public string Description;                  // 物品描述
        public int Price;                           // 物品价格

        public ItemType Type;                       // 物品类型
        public WeaponType WeaponType;               // 武器类型（只有物品为武器的时候有意义）

        // 主角可使用信息
        public HashSet<PlayerActorId> ApplicableActors;

        // 五灵属性
        public HashSet<ObjectElementType> ElementAttributes;

        public int AncientValue;                    // 古董价值

        public ItemSpecialType ItemSpecialType;     // 特殊类型
        public TargetRangeType TargetRangeType;     // 目标范围类型
        public PlaceOfUseType PlaceOfUseType;       // 使用地点类型

        // 角色属性影响类型和影响值
        public Dictionary<ActorAttributeType, AttributeImpact> AttributeImpacts;

        // 战斗状态影响类型和值
        public Dictionary<ActorCombatStateType, CombatStateImpact>  CombatStateImpacts;

        #if PAL3
            public int ComboCount;                      // 连击次数

            // 装备属性
            public short SpSavingPercentage;                // 气消耗减少百分比
            public short MpSavingPercentage;                // 神消耗减少百分比
            public short CriticalAttackAmplifyPercentage;   // 暴击伤害增加百分比
            public short SpecialSkillSuccessRate;           // 特技成功率增加百分比

            // 尸块属性
            public uint OreId;                      // 矿石ID
            public uint ProductId;                  // 炼制出的物品ID
            public int ProductPrice;                // 炼制出的物品价格

            // 合成属性
            public uint[] SynthesisMaterialIds;     // 两种合成材料的ID
            public uint SynthesisProductId;         // 合成出的物品ID
        #elif PAL3A
            public int Unknown1;
            public uint Unknown2;

            public short SpSavingPercentage;                // 气消耗减少百分比
            public short MpSavingPercentage;                // 神消耗减少百分比
            public short CriticalAttackAmplifyPercentage;   // 暴击伤害增加百分比
            public short SpecialSkillSuccessRate;           // 特技成功率增加百分比

            public PlayerActorId CreatorActorId;     // 制作角色ID
            public uint MaterialId;                  // 制作所需材料的ID

            public int ProductType;                  // 制作出的物品类型（0美食方，梦溪杂录，1装备图谱，花种，2武器图谱）
            public uint ProductId;                   // 制作出的物品ID
            public uint RequiredFavorValue;          // 制作所需的最低好感度
        #endif
    }

    [System.Serializable]
    public struct CombatActorInfo
    {
        public uint Id;                         // 战斗角色ID

        public CombatActorType Type;            // 战斗角色类型
        public string Description;              // 战斗角色描述
        public string ModelId;                  // 战斗角色模型ID
        public string IconId;                   // 战斗角色图标ID

        // 角色基础五灵属性值
        public Dictionary<ElementType, int> ElementAttributeValues;

        public string Name;                     // 战斗角色名称
        public int Level;                       // 战斗角色等级

        // 角色基础属性值
        public Dictionary<ActorAttributeType, int> AttributeValues;

        // 战斗状态影响类型
        public Dictionary<ActorCombatStateType, CombatStateImpactType>  CombatStateImpactTypes;

        public int MaxRound;                    // 指定最大回合数 （保存或者初始化的时候用，数据库文件里均为-1，主角为0）
        public int SpecialActionId;             // 特殊动作ID
        public float EscapeRate;                // 逃跑概率
        public ushort[] MainActorFavor;         // 对每个主角的好感（保存用）
        public int Experience;                  // 经验值
        public ushort Money;                    // 金钱（保存用）
        public uint NormalAttackModeId;         // 普通攻击动作模式ID
                                                // 916 917 921 922 926 927 => 行
                                                // 918 919 920 923 928 => 列

        public byte HeightLevel;                // 迷宫高度 0-2 对应 低 中 高
        public byte MoveRangeLevel;             // 运动范围 0-3 对应 不动 小 中 大
        public byte AttackRangeLevel;           // 索敌范围 0-2 对应 小 中 大
        public byte MoveSpeedLevel;             // 运动速度 0-3 对应 不动 小 中 大
        public byte ChaseSpeedLevel;            // 追踪速度 0-2 对应 小 中 大
        public uint[] SkillIds;                 // 怪物所会的仙术ID [0-2] [3]为Boss级法术
        public byte[] SkillLevels;              // 怪物所会的3种法术的等级 boss级法术无此属性
        public int SpImpactValue;               // 物理攻击对我方气的影响值

        public float[] Properties;              // 附加属性,主要用于战斗音效类型的判断

        public uint NormalLoot;                 // 普通战利品
        public short NormalLootCount;           // 普通战利品数量
        public uint CorpseId;                   // 尸块ID
        public uint CorpseSkillId;              // 触发尸块的技能ID
        public short CorpseSuccessRate;         // 尸块获得成功率
        public short StealableMoneyAmount;      // 可偷多少钱
        public uint StealableItemId;            // 可偷物品ID
        public short StealableItemCount;        // 可偷物品数量
        public short MoneyWhenKilled;           // 被击败时可得钱数
    }

    public struct SkillInfo
    {
        public uint Id;                                       // 技能ID
        public SkillType Type;                                // 技能类型
        public HashSet<ObjectElementType> ElementAttributes;  // 五灵属性
        public string Name;                                   // 技能名称
        public string Description;                            // 技能描述

        // 主角是否可以使用
        public HashSet<PlayerActorId> ApplicableActors;

        public TargetRangeType TargetRangeType;               // 目标范围类型
        public byte SpecialSkillId;                           // 特殊技能ID 0为无特殊效果 1开头为偷敌人东西

        // 角色属性影响类型和影响值
        public Dictionary<ActorAttributeType, AttributeImpact> AttributeImpacts;

        public byte SuccessRateLevel;                         // 施法成功率 0～10对应不约定的计算方式 其他为百分比

        // 战斗状态影响类型
        public Dictionary<ActorCombatStateType, CombatStateImpactType>  CombatStateImpactTypes;

        // 消耗属性类型
        public AttributeImpactType SpConsumeImpactType;
        public AttributeImpactType MpConsumeImpactType;

        public int SpConsumeValue;                            // 消耗气数值
        public int MpConsumeValue;                            // 消耗神数值

        // -1 无，(0精，10钱, 16蛊) [0精 1气 2神 3武 4防 5情 6经 7速 8运 9级 10钱 11水 12火 13风 14雷 15土 16蛊]
        public int SpecialConsumeType;                        // 特殊消耗
        public AttributeImpactType SpecialConsumeImpactType;  // 特殊消耗类型
        public int SpecialConsumeValue;                       // 特殊消耗数值

        public byte Level;                                    // 技能等级
        public byte[] TimesBeforeLevelUp;                     // 升级前需使用次数
        public byte RequiredActorLevel;                       // 使用所需角色等级
        public byte MagicLevel;                               // 本级法术等级
        public uint NextLevelSkillId;                         // 下一级法术ID
        public byte IsUsableOutsideCombat;                    // 是否可以在战斗外使用

        public uint[] CompositeSkillIds;                      // 可以合成的法术ID
        public uint[] CompositeRequiredSkillIds;              // 合成所需其他法术ID
        public byte[] CompositeRequiredSkillLevels;           // 合成所需其他法术等级
        public byte[] CompositeRequiredCurrentSkillLevels;    // 合成所需法术当前等级
        public byte[] CompositeRequiredActorLevels;           // 合成所需角色等级
        public byte CanTriggerComboSkill;                     // 是否可以触发合击技
    }

    public struct ComboSkillInfo
    {
        public string Name;                             // 合击技名称
        public uint Id;                                 // 合击技ID
        public uint[] MainActorRequirements;            // 需要的人(角色ID)

        // 需要每个人的五灵位置,于上面数组一一对应
        public ElementPositionRequirementType[] ElementPositionRequirements;

        public uint SkillId;                            // 发动技能ID
        public WeaponType[] WeaponTypeRequirements;     // 对应每个参战者的武器类型
        public ActorCombatStateType[] CombatStateRequirements;  // 对应每个参战者的战斗状态要求
        public string Description;                      // 合击技描述
        public TargetRangeType TargetRangeType;         // 使用范围类型

        // 角色属性影响类型和影响值
        public Dictionary<ActorAttributeType, AttributeImpact> AttributeImpacts;

        #if PAL3A
        public int Unknown;  // 三外新增数据 暂时未知
        #endif
    }

    // Pal3 Game Database file
    public sealed class GdbFile
    {
        public IDictionary<int, CombatActorInfo> CombatActorInfos { get; }
        public IDictionary<int, SkillInfo> SkillInfos { get; }
        public IDictionary<int, GameItemInfo> GameItemInfos { get; }
        public IDictionary<int, ComboSkillInfo> ComboSkillInfos { get; }

        public GdbFile(IDictionary<int, CombatActorInfo> combatActorInfos,
            IDictionary<int, SkillInfo> skillInfos,
            IDictionary<int, GameItemInfo> gameItemInfos,
            IDictionary<int, ComboSkillInfo> comboSkillInfos)
        {
            CombatActorInfos = combatActorInfos;
            SkillInfos = skillInfos;
            GameItemInfos = gameItemInfos;
            ComboSkillInfos = comboSkillInfos;
        }
    }
}