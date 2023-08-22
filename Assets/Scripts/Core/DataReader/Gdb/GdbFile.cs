// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Core.DataReader.Gdb
{
    using System.Collections.Generic;

    public enum ElementType
    {
        None    = 0,
        Water   = 1,    // 水
        Fire    = 2,    // 火
        Wind    = 3,    // 风
        Thunder = 4,    // 雷
        Earth   = 5,    // 土
    }

    public enum ItemType
    {
        None = 0,
        Healing,       // 治疗
        Throwable,     // 投掷
        Treasure,      // 仙宝
        Antique,       // 古董
        Ore,           // 矿石
        Plot,          // 剧情
        Cloth,         // 衣服
        Hat,           // 帽子
        Shoes,         // 鞋子
        Wearable,      // 饰品
        Weapon,        // 武器
        Corpse,        // 尸块
        Blueprint,     // 蓝图（梦溪杂录，美食方）
    }

    public enum WeaponType
    {
        None = 0,
        Spear,        // 长矛
        Sword,        // 剑
        Staff,        // 杖
        Machete,      // 刀
        Bow,          // 弓
        Spine,        // 刺
        Sickle,       // 镰刀
    }

    public enum SpecialType
    {
        None = 0,
        Poison,        // 随机中毒
        Flee,          // 100%逃跑
        Navigation,    // 引路蜂类
        LevelUp,       // 升级类（等级提高1）
    }

    public enum TargetRangeType
    {
        None = 0,
        FirstParty,        // 我方单体
        FirstPartyAll,     // 我方全体
        EnemyParty,        // 敌方单体
        EnemyPartyAll,     // 敌方全体
        EnemyPartyRow,     // 敌方一排
        EnemyPartyColumn,  // 敌方一列
    }

    public enum PlaceOfUseType
    {
        None = 0,
        Combat,        // 战斗中
        OutOfCombat,   // 非战斗中
        Anywhere,      // 任何地方
    }

    public enum ActorAttributeType
    {
        Hp,            // 精
        Sp,            // 气
        Mp,            // 神
        Attack,        // 攻击
        Defense,       // 防御
        Speed,         // 速度
        Luck,          // 幸运
        Water,         // 水
        Fire,          // 火
        Wind,          // 风
        Thunder,       // 雷
        Earth,         // 土
    }

    public enum CombatActorType
    {
        MainActor = 0,   // 主角
        Human,           // 人
        Monster,         // 妖
        Fairy,           // 仙
        God,             // 神
        Ghost,           // 鬼
        Demon            // 魔
    }

    public enum ActorCombatState
    {
        PoisonWind = 0,         // 风毒
        PoisonThunder,          // 雷毒
        PoisonWater,            // 水毒
        PoisonFire,             // 火毒
        PoisonEarth,            // 土毒
        AttackIncrease,         // 武 临时增加10%
        AttackDecrease,         // 武 临时降低10%
        DefenseIncrease,        // 防 临时增加10%
        DefenseDecrease,        // 防 临时降低10%
        LuckIncrease,           // 运 临时增加10%
        LuckDecrease,           // 运 临时降低10%
        SpeedIncrease,          // 速 临时增加10%
        SpeedDecrease,          // 速 临时降低10%
        Paralysis,              // 定(麻痹)
        Seal,                   // 封(封咒)
        Forbidden,              // 禁(禁忌)
        Sleep,                  // 眠(催眠)
        Chaos,                  // 乱(混乱)
        Madness,                // 狂(疯狂)
        Reflection,             // 镜(攻击反射)
        Evade,                  // 避(防避,物理攻击无效)
        Barrier,                // 界(结界,仙术攻击无效)
        Invisible,              // 隐形
        Dying,                  // 濒死
        Death,                  // 死亡
        PoisonResist,           // 避毒
        PoisonAny,              // 任意毒
        EvilResist,             // 辟邪
        DemonResist,            // 退魔
        GodsEye,                // 神眼
        ExpIncrease,            // 精进
    }

    public enum SkillType
    {
        StandardMagic = 0,    // 标准技能
        AssistMagic,          // 辅助技能
        DamageMagic,          // 伤害法术
        RecoverMagic,         // 恢复法术
        WeaponMagic,          // 武器技
    }

    public struct GameItemInfo
    {
        public uint Id;                          // 物品ID

        public string Name;                      // 物品名称
        public string ModelName;                 // 物品模型名称
        public string IconName;                  // 物品图标名称
        public string Description;               // 物品描述
        public int Price;                        // 物品价格

        public ItemType Type;                    // 物品类型
        public WeaponType WeaponType;            // 武器类型（只有物品为武器的时候有意义）

        public byte[] MainActorCanUse;           // 主角可使用信息
        public byte[] ElementProperties;         // 五灵属性

        public int AncientValue;                 // 古董价值

        public SpecialType SpecialType;          // 特殊类型
        public TargetRangeType TargetRangeType;  // 目标范围类型
        public PlaceOfUseType PlaceOfUseType;    // 使用地点类型

        public byte[] AttributeImpactType;       // 角色属性影响类型 (0绝对值，1百分比值，2恢复到上限，3增加上限值)
        public short[] AttributeImpactValue;     // 角色属性影响值

        public byte[] CombatStateImpactType;     // 战斗状态影响类型（0不影响，1增加，2解除）
        public short[] CombatStateImpactValue;   // 战斗状态值

        public int ComboCount;                   // 连击次数

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
    }

    public struct CombatActorInfo
    {
        public uint Id;                         // 战斗角色ID

        public CombatActorType Type;            // 战斗角色类型
        public string Description;              // 战斗角色描述
        public string ModelId;                  // 战斗角色模型ID
        public string IconId;                   // 战斗角色图标ID

        public int[] ElementProperties;         // 五灵属性
        public string Name;                     // 战斗角色名称
        public int Level;                       // 战斗角色等级
        public int[] AttributeValue;            // 角色属性值
        public byte[] CombatStateImpactType;    // 战斗状态影响类型（0不影响，1增加，2解除）
        public int RoundNumber;                 // 指定回合数
        public int SpecialActionId;             // 特殊动作ID
        public float EscapeRate;                // 逃跑概率
        public ushort[] MainActorFavor;         // 对每个主角的好感
        public int Experience;                  // 经验值
        public ushort Money;                    // 金钱
        public uint NormalAttackActionId;       // 普通攻击动作ID

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
        public uint Id;                                     // 技能ID
        public SkillType Type;                              // 技能类型
        public int[] ElementProperties;                     // 五灵属性
        public string Name;                                 // 技能名称
        public string Description;                          // 技能描述
        public byte[] MainActorCanUse;                      // 主角是否可以使用

        public TargetRangeType TargetRangeType;             // 目标范围类型
        public byte SpecialSkillId;                         // 特殊技能ID 0为无特殊效果 1开头为偷敌人东西
        public byte[] AttributeImpactType;                  // 属性影响类型 增加(或减少)数值的类型(0为绝对值,1为百分数,2为恢复到上限,3为增加上限值)
        public short[] AttributeImpactValue;                // 属性影响数值

        public byte SuccessRateLevel;                       // 施法成功率 0～10对应不约定的计算方式 其他为百分比
        public short[] CombatStateImpactType;               // 战斗状态影响类型（0不影响，1增加，2解除）
        public int[] ConsumeAttributeType;                  // 消耗属性类型
        public int[] ConsumeAttributeKind;                  // 消耗属性种类数值 0-消耗气,1-消耗神,2-特殊消耗[精气神武防,情经速运级,钱水火风雷土蛊];
        public byte SpecialConsumeType;                     // 特殊消耗类型 增加(或减少)数值的类型(0为绝对值,1为百分数,2为恢复到上限,3为增加上限值)
        public int SpecialConsumeValue;                     // 特殊消耗数值

        public byte Level;                                  // 技能等级
        public byte[] TimesBeforeLevelUp;                   // 升级前需使用次数
        public byte RequiredActorLevel;                     // 使用所需角色等级
        public byte MagicLevel;                             // 本级法术等级
        public uint NextLevelSkillId;                       // 下一级法术ID
        public byte IsUsableOutsideCombat;                  // 是否可以在战斗外使用

        public uint[] CompositeSkillIds;                    // 可以合成的法术ID
        public uint[] CompositeRequiredSkillIds;            // 合成所需其他法术ID
        public byte[] CompositeRequiredSkillLevels;         // 合成所需其他法术等级
        public byte[] CompositeRequiredCurrentSkillLevels;  // 合成所需法术当前等级
        public byte[] CompositeRequiredActorLevels;         // 合成所需角色等级
        public byte CanTriggerComboSkill;                   // 是否可以触发合击技
    }

    public struct ComboSkillInfo
    {
        public string Name;                             // 合击技名称
        public uint Id;                                 // 合击技ID
        public uint[] MainActorRequirements;            // 需要的人
        public byte[] ElementPositionRequirements;      // 需要每个人的五灵位置,于上面数组一一对应[0-任意 1-水...6-中 7-风火雷[任意] 8-水火土[任意]]
        public uint SkillId;                            // 发动技能ID
        public byte[] WeaponTypeRequirements;           // 对应每个参战者的武器类型
        public int[] CombatStateRequirements;           // 对应每个参战者的战斗状态要求
        public string Description;                      // 合击技描述
        public TargetRangeType TargetRangeType;         // 使用范围类型
        public byte[] AttributeImpactType;              // 属性影响类型 增加(或减少)数值的类型(0为绝对值,1为百分数,2为恢复到上限,3为增加上限值)
        public short[] AttributeImpactValue;            // 属性影响数值

        #if PAL3A
        public int Unknown;  // 三外特有数据 暂时未知 猜测是需要气的总数值？
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