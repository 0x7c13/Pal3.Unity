// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Core.Contracts
{
    [System.Serializable]
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

    [System.Serializable]
    public enum WeaponArmType
    {
        None,
        LeftHanded,
        RightHanded
    }

    [System.Serializable]
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

    [System.Serializable]
    public enum ItemSpecialType
    {
        None = 0,
        Poison,        // 随机中毒
        Flee,          // 100%逃跑
        Navigation,    // 引路蜂类
        LevelUp,       // 升级类（等级提高1）
    }
}