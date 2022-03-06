// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.MetaData
{
    using System.Collections.Generic;

    #if PAL3
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
    public enum PlayerActorId
    {
        NanGongHuang  =  0,   // 南宫煌
        WenHui        =  1,   // 温慧
        WangPengXu    =  2,   // 王蓬絮
        XingXuan      =  3,   // 星璇
        LeiYuanGe     =  4,   // 雷元戈
        TaoZi         =  5,   // 王蓬絮兽形
    }
    #endif

    public enum ActorActionType
    {
        // 基本动作
        Stand = 0,        // 站立
        Walk,             // 行走
        Run,              // 奔跑
        Back,             // 后退
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

    public static class ActorConstants
    {
        public const int PlayerActorVirtualID = -1; // or 255 as byte value

        public const float ActorClimbSpeed = 2f;

        public const string LongKuiHumanModeActorName = "105";
        public const string LongKuiGhostModeActorName = "106";

        public const string MonsterIdleAction = "z0";
        public const string MonsterWalkAction = "z4";


        #if PAL3
        public static readonly Dictionary<PlayerActorId, string> MainActorNameMap = new()
        {
            {PlayerActorId.JingTian,  "101"},
            {PlayerActorId.XueJian,   "104"},
            {PlayerActorId.LongKui,   "105"},  // 105 人形 106 鬼形
            {PlayerActorId.ZiXuan,    "107"},
            {PlayerActorId.ChangQing, "102"},
            {PlayerActorId.HuaYing,   "109"}
        };
        #elif PAL3A
        public static readonly Dictionary<PlayerActorId, string> MainActorNameMap = new()
        {
            {PlayerActorId.NanGongHuang,  "101"},  // 煌大仙狼形
            {PlayerActorId.WenHui,        "102"},
            {PlayerActorId.WangPengXu,    "103"},
            {PlayerActorId.XingXuan,      "104"},
            {PlayerActorId.LeiYuanGe,     "105"},
            {PlayerActorId.TaoZi,         "106"},  // 桃子兽形
        };
        #endif

        //每个动作对应的.A_Q模型中的名字
        public static readonly Dictionary<ActorActionType, string> ActionNames = new ()
        {
            // 基本动作
            { ActorActionType.Stand,             "c01"},   // 站立
            { ActorActionType.Walk,              "c02"},   // 行走
            { ActorActionType.Run,               "c03"},   // 奔跑
            { ActorActionType.Back,              "c15"},   // 后退
            { ActorActionType.Jump,              "c04"},   // 起跳
            { ActorActionType.Climb,             "c05"},   // 攀爬
            { ActorActionType.ClimbDown,         "c052"},  // 向下攀爬
            { ActorActionType.Push,              "c06"},   // 推
            { ActorActionType.Skill,             "c07"},   // 技能
            { ActorActionType.Tire,              "c08"},   // 疲倦
            { ActorActionType.Seat,              "c09"},   // 坐
            { ActorActionType.Check,             "c10"},   // 调查
            { ActorActionType.Sleep,             "c11"},   // 睡觉
            { ActorActionType.Shake,             "c12"},   // 摇头
            { ActorActionType.Nod,               "c13"},   // 点头
            { ActorActionType.Wave,              "c14"},   // 摆手

            // 剧情动作
            { ActorActionType.Signature,         "j01"},   // 招牌
            { ActorActionType.Shocked,           "j02"},   // 惊吓
            { ActorActionType.SeatDown,          "j03"},   // 下蹲
            { ActorActionType.SeatUp,            "j04"},   // 坐起
            { ActorActionType.Point,             "j05"},   // 指点
            { ActorActionType.Kick,              "j06"},   // 踢
            { ActorActionType.Salute,            "j07"},   // 行礼
            { ActorActionType.Tease,             "j08"},   // 挑逗
            { ActorActionType.Snuggle,           "j09"},   // 依偎
            { ActorActionType.Whining,           "j10"},   // 撒娇
            { ActorActionType.Anger,             "j11"},   // 发怒
            { ActorActionType.Dose,              "j12"},   // 服药
            { ActorActionType.J13,               "j13"},   // 得意

            // 战斗动作
            { ActorActionType.Attack1,		    "z01"},   // 攻击1
            { ActorActionType.Attack2,		    "z02"},   // 攻击2
            { ActorActionType.AttackMove,	    "z03"},   // 攻击移动
            { ActorActionType.PreAttack,        "z04"},   // 备战
            { ActorActionType.BeAttack,		    "z05"},   // 被攻击
            { ActorActionType.MagicWait,        "z061"},  // 仙术预备
            { ActorActionType.Magic,            "z06"},   // 仙术
            { ActorActionType.Skill1,		    "z09"},   // 特技1
            { ActorActionType.Skill2,		    "z10"},   // 特技2
            { ActorActionType.UseItemRecover,   "z11"},   // 恢复物品
            { ActorActionType.UseItemAttack,    "z12"},   // 攻击物品
            { ActorActionType.WeaponSkill,	    "z13"},   // 武器技
            { ActorActionType.StartDefence,	    "z14"},   // 开始防御
            { ActorActionType.Defence,		    "z141"},  // 防御
            { ActorActionType.DefenceBeAttack,  "z142"},  // 攻击被防住
            { ActorActionType.Dodge,            "z15"},   // 回避
            { ActorActionType.Freeze,           "z16"},   // 眠定
            { ActorActionType.Dying,            "z17"},   // 濒死
            { ActorActionType.Dead,			    "z18"},   // 死亡
            { ActorActionType.Flee,			    "z19"},   // 逃跑等到
            { ActorActionType.Win,			    "z20"},   // 胜利

            // NPC,怪物动作
            { ActorActionType.NpcStand1,        "z1"},    // 站立1
            { ActorActionType.NpcStand2,        "z2"},    // 站立2
            { ActorActionType.NpcWalk,	        "z3"},    // 行走
            { ActorActionType.NpcAttack,        "g"},     // 攻击
            { ActorActionType.NpcBeAttacked,    "b"},     // 攻击
            { ActorActionType.NpcMagic1,        "x1"},    // 仙术1
            { ActorActionType.NpcMagic2,        "x2"},    // 仙术2
            { ActorActionType.NpcMagic3,        "x3"},    // 仙术3
            { ActorActionType.NpcRun,           "z3"},    // 跑,用走代替
        };

        public static readonly HashSet<ActorActionType> ActionWithoutShadow = new()
        {
            ActorActionType.Sleep,
            ActorActionType.SeatUp,
            ActorActionType.SeatDown,
            ActorActionType.Dead,
        };
    }
}