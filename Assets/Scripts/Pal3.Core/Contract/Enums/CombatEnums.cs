// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2025, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Core.Contract.Enums
{
    [System.Serializable]
    public enum ElementType
    {
        None    = 0,
        Water   = 1,    // 水
        Fire    = 2,    // 火
        Wind    = 3,    // 风
        Thunder = 4,    // 雷
        Earth   = 5,    // 土
    }

    [System.Serializable]
    public enum ElementPositionRequirementType
    {
        Any     = 0,            // 任意位置
        Water   = 1,            // 水
        Fire    = 2,            // 火
        Wind    = 3,            // 风
        Thunder = 4,            // 雷
        Earth   = 5,            // 土
        Middle  = 6,            // 中
        WindFireThunder = 7,    // 风火雷[任意]
        WaterFireEarth = 8,     // 水火土[任意]
    }

    [System.Serializable]
    public enum TargetRangeType
    {
        None = 0,
        FirstPartySingle,    // 我方单体
        FirstPartyAll,       // 我方全体
        EnemyPartySingle,    // 敌方单体
        EnemyPartyAll,       // 敌方全体
        EnemyPartyOneRow,    // 敌方一排
        EnemyPartyOneColumn, // 敌方一列
    }

    [System.Serializable]
    public enum PlaceOfUseType
    {
        None = 0,
        Combat,        // 战斗中
        OutOfCombat,   // 非战斗中
        Anywhere,      // 任何地方
    }

    [System.Serializable]
    public enum ActorAttributeType
    {
        Hp = 0,        // 精
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

    [System.Serializable]
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

    [System.Serializable]
    public enum ActorCombatStateType
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

    [System.Serializable]
    public enum SkillType
    {
        StandardMagic = 0,    // 标准技能
        AssistMagic,          // 辅助技能
        DamageMagic,          // 伤害法术
        RecoverMagic,         // 恢复法术
        WeaponMagic,          // 武器技
    }

    [System.Serializable]
    public enum CombatStateImpactType
    {
        None = 0,         // 不影响
        Increase,         // 增加
        Remove,           // 解除
    }

    [System.Serializable]
    public enum AttributeImpactType
    {
        Absolute = 0,         // 绝对值
        Percentage,           // 百分比
        RecoverToMax,         // 恢复到上限
        IncreaseMax,          // 增加上限值
    }
}