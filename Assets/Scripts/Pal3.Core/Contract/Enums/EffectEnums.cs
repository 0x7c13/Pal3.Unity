// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2024, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Core.Contract.Enums
{
    [System.Serializable]
    public enum ActorEmojiType
    {
        None = 0,
        Sleepy,     // 睡
        Shock,      // 惊
        Doubt,      // 疑
        Anger,      // 怒
        Happy,      // 喜
        Heart,      // 心
        Sweat,      // 汗
        Bother,     // 乱
        Anxious,    // 急
        Cry,        // 泣
        Dizzy,      // 晕
        #if PAL3A
        Speechless  // 无语
        #endif
    }

    [System.Serializable]
    public enum GraphicsEffectType
    {
        None        = -1,
        PoisonFog   =  0,  // 雪见.瘴气
        Portal      =  1,  // 传送点
        SavePoint   =  2,  // 存盘点
        Fire        =  3,  // 火焰类特效
        Combat      =  4,  // 战斗特效
    }

    [System.Serializable]
    public enum FireEffectType
    {
        Type1 = 0,
        Type2,
        Type3,
        Type4,
        Type5,
        #if PAL3A
        Type6,
        #endif
    }
}