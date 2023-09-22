// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Core.Contract.Enums
{
    #if PAL3
    [System.Serializable]
    public enum PlayerActorId
    {
        JingTian  =  0,   // 景天
        XueJian   =  1,   // 雪见
        LongKui   =  2,   // 龙葵
        ZiXuan    =  3,   // 紫萱
        ChangQing =  4,   // 长卿
        HuaYing   =  5,   // 花盈
    }
    #elif PAL3A
    [System.Serializable]
    public enum PlayerActorId
    {
        NanGongHuang  =  0,   // 南宫煌
        WenHui        =  1,   // 温慧
        WangPengXu    =  2,   // 王蓬絮
        XingXuan      =  3,   // 星璇
        LeiYuanGe     =  4,   // 雷元戈
        TaoZi         =  5,   // 王蓬絮兽形
    }
    [System.Serializable]
    public enum FengYaSongActorId
    {
        Feng       = 0xFF - 3,  // Use last 3 IDs for FengYaSong actors
        Ya         = 0xFF - 2,  // to avoid conflict with other actors
        Song       = 0xFF - 1,
    }
    #endif

    #if PAL3
    [System.Serializable]
    public enum ActorActionType
    {
        // 基本动作
        Stand = 0,        // 站立
        Walk,             // 行走
        Run,              // 奔跑
        StepBack,         // 后退
        Jump,             // 起跳
        Climb,            // 攀爬
        ClimbDown,        // 向下爬
        Push,             // 推
        Skill,            // 技能
        Tire,             // 疲倦
        Seat,             // 坐
        Check,            // 调查
        Sleep,            // 睡觉
        Shake,            // 摇头
        Nod,              // 点头
        Wave,             // 摆手

        // 剧情动作
        Signature,        // 招牌
        Shocked,          // 惊吓
        SeatDown,         // 下蹲
        SeatUp,           // 坐起
        Point,            // 指点
        Kick,             // 踢
        Salute,           // 行礼
        Tease,            // 挑逗
        Snuggle,          // 依偎
        Whining,          // 撒娇
        Anger,            // 发怒
        Dose,             // 服药
        J13,              // 得意

        // 战斗动作
        Attack1,		  // 攻击1
        Attack2,		  // 攻击2
        AttackMove,		  // 攻击移动
        PreAttack,		  // 备战
        BeAttack,		  // 被攻击
        MagicWait,		  // 仙术预备
        Magic,			  // 仙术
        Skill1,			  // 特技1
        Skill2,			  // 特技2
        UseItemRecover,	  // 恢复物品
        UseItemAttack,	  // 攻击物品
        WeaponSkill,	  // 武器技
        StartDefence,	  // 开始防御
        Defence,		  // 防御
        DefenceBeAttack,  // 攻击被防住
        Dodge,			  // 回避
        Freeze,			  // 眠定
        Dying,			  // 濒死
        Dead,			  // 死亡
        Flee,			  // 逃跑等到
        Win,			  // 胜利

        // NPC,怪物动作
        NpcStand1,	      // 站立1
        NpcStand2,	      // 站立2
        NpcWalk,	      // 行走
        NpcAttack,	      // 攻击
        NpcBeAttacked,    // 被攻击
        NpcMagic1,        // 仙术1
        NpcMagic2,        // 仙术2
        NpcMagic3,        // 仙术3
        NpcRun,           // 跑
    }
    #elif PAL3A
    [System.Serializable]
    public enum ActorActionType
    {
        // 基本动作
        Stand = 0,        // 站立
        Walk,             // 行走
        Run,              // 奔跑
        StepBack,         // 后退
        Jump,             // 起跳
        Climb,            // 攀爬
        ClimbDown,        // 向下爬
        Push,             // 推
        Skill,            // 技能
        Tire,             // 疲倦
        Seat,             // 坐
        Check,            // 调查
        Sleep,            // 睡觉
        Shake,            // 摇头
        Nod,              // 点头
        Wave,             // 摆手

        // 剧情动作
        Threaten,         // 威胁
        Shocked,          // 惊吓
        BendOver,         // 下蹲
        SeatUp,           // 坐起
        Point,            // 指点
        Kick,             // 踢
        Salute,           // 行礼
        Tease,            // 挑逗
        Snuggle,          // 依偎
        Whining,          // 撒娇
        Anger,            // 发怒
        Dose,             // 服药
        J13,              // 得意

        // 战斗动作
        Attack1,		  // 攻击1
        Attack2,		  // 攻击2
        AttackMove,		  // 攻击移动
        PreAttack,		  // 备战
        BeAttack,		  // 被攻击
        MagicWait,		  // 仙术预备
        Magic,            // 仙术
        Skill1,           // 特技1
        Skill2,           // 特技2
        UseItemRecover,	  // 恢复物品
        UseItemAttack,	  // 攻击物品
        WeaponSkill,	  // 武器技
        StartDefence,	  // 开始防御
        Defence,		  // 防御
        DefenceBeAttack,  // 攻击被防住
        Dodge,			  // 回避
        Freeze,			  // 眠定
        Dying,			  // 濒死
        Dead,			  // 死亡
        Flee,			  // 逃跑等到
        Win,			  // 胜利

        // NPC,怪物动作
        NpcStand1,	      // 站立1
        NpcStand2,	      // 站立2
        NpcWalk,	      // 行走
        NpcAttack,	      // 攻击
        NpcBeAttacked,    // 被攻击
        NpcMagic1,        // 仙术1
        NpcMagic2,        // 仙术2
        NpcMagic3,        // 仙术3
        NpcRun,           // 跑

        // 仙剑三外传新增动作
        LevelUp,          // 升级
        Transform,        // 变身
        Attack21,         // 扔八卦的第一段动作
        Attack22,         // 扔八卦的第二段动作
        Attack23          // 扔八卦的第三段动作
    }
    #endif
}