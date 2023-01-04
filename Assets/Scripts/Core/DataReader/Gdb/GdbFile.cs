// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Core.DataReader.Gdb
{
    using System.Collections.Generic;

    public enum WuLingType
    {
        Water,    // 水
        Fire,     // 火
        Wind,     // 风
        Thunder,  // 雷
        Earth,    // 土
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
        Qi,            // 气
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

    public struct GameItem
    {
        public uint Id;                         // 物品ID

        public string Name;                     // 物品名称
        public string ModelName;                // 物品模型名称
        public string IconName;                 // 物品图标名称
        public string Description;              // 物品描述
        public int Price;                       // 物品价格

        public ItemType Type;                   // 物品类型
        public WeaponType WeaponType;           // 武器类型（只有物品为武器的时候有意义）

        public byte[] MainActorCanUse;          // 主角可使用信息
        public byte[] WuLing;                   // 五灵属性

        public int AncientValue;                // 古董价值

        public SpecialType SpecialType;         // 特殊类型
        public TargetRangeType TargetRangeType; // 目标范围类型
        public PlaceOfUseType PlaceOfUseType;   // 使用地点类型

        public byte[] AttributeImpactType;      // 角色属性影响类型 (0绝对值，1百分比值，2恢复到上限，3增加上限值)

        // Empty byte between the memory addresses to align the data in memory.
        // SpecialType, TargetRangeType and PlaceOfUseType take 3 bytes in memory, thus we need to add 1 byte here
        // to align the data in memory (4 bytes).
        // Note: The padding byte is added after AttributeImpactType instead of before based on observation.
        internal byte PaddingByte;

        public short[] AttributeImpactValue;    // 角色属性影响值

        public byte[] FightStateImpactType;     // 战斗状态影响类型（0不影响，1增加，2解除）
        public short[] FightStateImpactValue;   // 战斗状态值

        public int ComboCount;                  // 连击次数

        // 装备属性
        public short QiSavingPercentage;                  // 气消耗减少百分比
        public short MpSavingPercentage;                  // 神消耗减少百分比
        public short CriticalAttackAmplifyPercentage;     // 暴击伤害增加百分比
        public short SpecialSkillSuccessRate;             // 特技成功率增加百分比

        // 尸块属性
        public uint OreId;                      // 矿石ID
        public uint ProductId;                  // 炼制出的物品ID
        public int ProductPrice;                // 炼制出的物品价格

        // 合成属性
        public uint[] SynthesisMaterialIds;     // 两种合成材料的ID
        public uint SynthesisProductId;         // 合成出的物品ID
    }

    // Pal3 Game Database file
    public class GdbFile
    {
        public Dictionary<int, GameItem> GameItems { get; } // Game item Id -> Game item

        public GdbFile(Dictionary<int, GameItem> gameItems)
        {
            GameItems = gameItems;
        }
    }
}