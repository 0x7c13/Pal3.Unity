// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.MetaData
{
    using System.Collections.Generic;

    public enum WeaponArmType
    {
        None,
        LeftHanded,
        RightHanded
    }
    
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
    public enum FengYaSongActorId
    {
        Feng       = 250,
        Ya         = 251,
        Song       = 252,
    }
    #endif

    #if PAL3
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
    #elif PAL3A
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

        // 仙剑三外传新增动作
        LevelUp,          // 升级
        Transform,        // 变身
        Attack21,         // 扔八卦的第一段动作
        Attack22,         // 扔八卦的第二段动作
        Attack23          // 扔八卦的第三段动作
    }
    #endif

    public static class ActorConstants
    {
        public const int PlayerActorVirtualID = -1; // or 255 as byte value

        public const float ActorClimbSpeed = 2f;

        #if PAL3
        public const string LongKuiHumanModeActorName = "105";
        public const string LongKuiGhostModeActorName = "106";
        #elif PAL3A
        public const string NanGongHuangHumanModeActorName = "101";
        public const string NanGongHuangWolfModeActorName = "107";
        #endif

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
            {PlayerActorId.NanGongHuang,  "101"},  // 101 人形 107 狼形
            {PlayerActorId.WenHui,        "102"},
            {PlayerActorId.WangPengXu,    "103"},
            {PlayerActorId.XingXuan,      "104"},
            {PlayerActorId.LeiYuanGe,     "105"},
            {PlayerActorId.TaoZi,         "106"},  // 桃子兽形
        };
        public static readonly Dictionary<FengYaSongActorId, string> FengYaSongActorNameMap = new()
        {
            {FengYaSongActorId.Feng,  "110"},  // 风
            {FengYaSongActorId.Ya,    "108"},  // 雅
            {FengYaSongActorId.Song,  "109"},  // 颂
        };
        #endif
        
        #if PAL3
        public static readonly Dictionary<string, string> MainActorWeaponMap = new()
        {
            { "101", "JT13" },
            { "102", "WCA" },
            { "104", "WXF" },
            { "105", "WLF" },
            { "107", "WZF" },
        };
        #elif PAL3A
        public static readonly Dictionary<string, string> MainActorWeaponMap = new()
        {
            { "101", "WHA" },
            { "102", "WWA" },
            { "104", "WXA" },
            { "105", "WLA" },
        };
        #endif
        
        //每个动作对应的.A_Q模型中的名字
        #if PAL3
        public static readonly Dictionary<ActorActionType, string> ActionNames = new()
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
            { ActorActionType.Attack1,		     "z01"},   // 攻击1
            { ActorActionType.Attack2,		     "z02"},   // 攻击2
            { ActorActionType.AttackMove,	     "z03"},   // 攻击移动
            { ActorActionType.PreAttack,         "z04"},   // 备战
            { ActorActionType.BeAttack,		     "z05"},   // 被攻击
            { ActorActionType.MagicWait,         "z061"},  // 仙术预备
            { ActorActionType.Magic,             "z06"},   // 仙术
            { ActorActionType.Skill1,		     "z09"},   // 特技1
            { ActorActionType.Skill2,		     "z10"},   // 特技2
            { ActorActionType.UseItemRecover,    "z11"},   // 恢复物品
            { ActorActionType.UseItemAttack,     "z12"},   // 攻击物品
            { ActorActionType.WeaponSkill,	     "z13"},   // 武器技
            { ActorActionType.StartDefence,	     "z14"},   // 开始防御
            { ActorActionType.Defence,		     "z141"},  // 防御
            { ActorActionType.DefenceBeAttack,   "z142"},  // 攻击被防住
            { ActorActionType.Dodge,             "z15"},   // 回避
            { ActorActionType.Freeze,            "z16"},   // 眠定
            { ActorActionType.Dying,             "z17"},   // 濒死
            { ActorActionType.Dead,			     "z18"},   // 死亡
            { ActorActionType.Flee,			     "z19"},   // 逃跑等到
            { ActorActionType.Win,			     "z20"},   // 胜利

            // NPC,怪物动作
            { ActorActionType.NpcStand1,         "z1"},    // 站立1
            { ActorActionType.NpcStand2,         "z2"},    // 站立2
            { ActorActionType.NpcWalk,	         "z3"},    // 行走
            { ActorActionType.NpcAttack,         "g"},     // 攻击
            { ActorActionType.NpcBeAttacked,     "b"},     // 攻击
            { ActorActionType.NpcMagic1,         "x1"},    // 仙术1
            { ActorActionType.NpcMagic2,         "x2"},    // 仙术2
            { ActorActionType.NpcMagic3,         "x3"},    // 仙术3
            { ActorActionType.NpcRun,            "z3"},    // 跑,用走代替
        };
        #elif PAL3A
        public static readonly Dictionary<ActorActionType, string> ActionNames = new()
        {
            // 基本动作
            { ActorActionType.Stand,             "c01"},   // 站立
            { ActorActionType.Walk,              "c02"},   // 行走
            { ActorActionType.Run,               "c03"},   // 奔跑
            { ActorActionType.Back,              "c15"},   // 后退
            { ActorActionType.Jump,              "c05"},   // 起跳
            { ActorActionType.Climb,             "c04a"},  // 向上攀爬
            { ActorActionType.ClimbDown,         "c04b"},  // 向下攀爬
            { ActorActionType.Push,              "c013"},  // 推
            { ActorActionType.Skill,             "c06"},   // 技能
            { ActorActionType.Tire,              "c07"},   // 疲倦
            { ActorActionType.Seat,              "c12"},   // 坐
            { ActorActionType.Check,             "c08"},   // 调查
            { ActorActionType.Sleep,             "j04"},   // 睡觉
            { ActorActionType.Shake,             "c09"},   // 摇头
            { ActorActionType.Nod,               "c10"},   // 点头
            { ActorActionType.Wave,              "c11"},   // 摆手

            // 剧情动作
            { ActorActionType.Threaten,          "j15"},   // 威胁
            { ActorActionType.Shocked,           "j06"},   // 惊吓
            { ActorActionType.BendOver,          "j07"},   // 下蹲
            { ActorActionType.SeatUp,            "j02"},   // 坐起
            { ActorActionType.Point,             "j05"},   // 指点
            { ActorActionType.Kick,              "j06"},   // 踢
            { ActorActionType.Salute,            "j09"},   // 行礼
            { ActorActionType.Tease,             "j08"},   // 挑逗
            { ActorActionType.Snuggle,           "j09"},   // 依偎
            { ActorActionType.Whining,           "j10"},   // 撒娇
            { ActorActionType.Anger,             "j11"},   // 发怒
            { ActorActionType.Dose,              "j03"},   // 服药
            { ActorActionType.J13,               "j12"},   // 得意

            // 战斗动作
            { ActorActionType.Attack1,		     "z01"},   // 攻击1
            { ActorActionType.Attack2,		     "z02"},   // 攻击2
            { ActorActionType.AttackMove,	     "z04"},   // 攻击移动
            { ActorActionType.PreAttack,         "z03"},   // 备战
            { ActorActionType.BeAttack,		     "z05"},   // 被攻击
            { ActorActionType.MagicWait,         "z081"},  // 仙术预备
            { ActorActionType.Magic,             "z08"},   // 仙术
            { ActorActionType.Skill1,		     "z17"},   // 特技1
            { ActorActionType.Skill2,		     "z18"},   // 特技2
            { ActorActionType.UseItemRecover,    "z09"},   // 恢复物品
            { ActorActionType.UseItemAttack,     "z10"},   // 攻击物品
            { ActorActionType.WeaponSkill,	     "z19"},   // 武器技
            { ActorActionType.StartDefence,	     "z061"},  // 开始防御
            { ActorActionType.Defence,		     "z062"},  // 防御
            { ActorActionType.DefenceBeAttack,   "z063"},  // 攻击被防住
            { ActorActionType.Dodge,             "z07"},   // 回避
            { ActorActionType.Freeze,            "z11"},   // 眠定
            { ActorActionType.Dying,             "z12"},   // 濒死
            { ActorActionType.Dead,			     "z13"},   // 死亡
            { ActorActionType.Flee,			     "z14"},   // 逃跑等到
            { ActorActionType.Win,			     "z15"},   // 胜利

            // NPC,怪物动作
            { ActorActionType.NpcStand1,         "z1"},    // 站立1
            { ActorActionType.NpcStand2,         "z2"},    // 站立2
            { ActorActionType.NpcWalk,	         "z3"},    // 行走
            { ActorActionType.NpcAttack,         "g"},     // 攻击
            { ActorActionType.NpcBeAttacked,     "b"},     // 攻击
            { ActorActionType.NpcMagic1,         "x1"},    // 仙术1
            { ActorActionType.NpcMagic2,         "x2"},    // 仙术2
            { ActorActionType.NpcMagic3,         "x3"},    // 仙术3
            { ActorActionType.NpcRun,            "z3"},    // 跑,用走代替
            
            // 仙剑三外传新增动作
            { ActorActionType.LevelUp,          "z16"},    // 升级
            { ActorActionType.Transform,        "z20"},    // 变身
            { ActorActionType.Attack21,         "z021"},   // 扔八卦的第一段动作
            { ActorActionType.Attack22,	        "z022"},   // 扔八卦的第二段动作
            { ActorActionType.Attack23,	        "z023"},   // 扔八卦的第三段动作
        };
        #endif

        #if PAL3
        public static readonly Dictionary<ActorActionType, WeaponArmType> ActionNameToWeaponArmTypeMap = new()
        {
            // 基本动作
            { ActorActionType.Stand,             WeaponArmType.None },   // 站立
            { ActorActionType.Walk,              WeaponArmType.None },   // 行走
            { ActorActionType.Run,               WeaponArmType.None },   // 奔跑
            { ActorActionType.Back,              WeaponArmType.None },   // 后退
            { ActorActionType.Jump,              WeaponArmType.None },   // 起跳
            { ActorActionType.Climb,             WeaponArmType.None },   // 攀爬
            { ActorActionType.ClimbDown,         WeaponArmType.None },   // 向下攀爬
            { ActorActionType.Push,              WeaponArmType.None },   // 推
            { ActorActionType.Skill,             WeaponArmType.None },   // 技能
            { ActorActionType.Tire,              WeaponArmType.None },   // 疲倦
            { ActorActionType.Seat,              WeaponArmType.None },   // 坐
            { ActorActionType.Check,             WeaponArmType.None },   // 调查
            { ActorActionType.Sleep,             WeaponArmType.None },   // 睡觉
            { ActorActionType.Shake,             WeaponArmType.None },   // 摇头
            { ActorActionType.Nod,               WeaponArmType.None },   // 点头
            { ActorActionType.Wave,              WeaponArmType.None },   // 摆手

            // 剧情动作
            { ActorActionType.Signature,         WeaponArmType.None },   // 招牌
            { ActorActionType.Shocked,           WeaponArmType.None },   // 惊吓
            { ActorActionType.SeatDown,          WeaponArmType.None },   // 下蹲
            { ActorActionType.SeatUp,            WeaponArmType.None },   // 坐起
            { ActorActionType.Point,             WeaponArmType.None },   // 指点
            { ActorActionType.Kick,              WeaponArmType.None },   // 踢
            { ActorActionType.Salute,            WeaponArmType.None },   // 行礼
            { ActorActionType.Tease,             WeaponArmType.None },   // 挑逗
            { ActorActionType.Snuggle,           WeaponArmType.None },   // 依偎
            { ActorActionType.Whining,           WeaponArmType.None },   // 撒娇
            { ActorActionType.Anger,             WeaponArmType.None },   // 发怒
            { ActorActionType.Dose,              WeaponArmType.None },   // 服药
            { ActorActionType.J13,               WeaponArmType.None },   // 得意

            // 战斗动作
            { ActorActionType.Attack1,		     WeaponArmType.RightHanded },   // 攻击1
            { ActorActionType.Attack2,		     WeaponArmType.RightHanded },   // 攻击2
            { ActorActionType.AttackMove,	     WeaponArmType.RightHanded },   // 攻击移动
            { ActorActionType.PreAttack,         WeaponArmType.RightHanded },   // 备战
            { ActorActionType.BeAttack,		     WeaponArmType.None        },   // 被攻击
            { ActorActionType.MagicWait,         WeaponArmType.RightHanded },   // 仙术预备
            { ActorActionType.Magic,             WeaponArmType.RightHanded },   // 仙术
            { ActorActionType.Skill1,		     WeaponArmType.RightHanded },   // 特技1
            { ActorActionType.Skill2,		     WeaponArmType.RightHanded },   // 特技2
            { ActorActionType.UseItemRecover,    WeaponArmType.None        },   // 恢复物品
            { ActorActionType.UseItemAttack,     WeaponArmType.None        },   // 攻击物品
            { ActorActionType.WeaponSkill,	     WeaponArmType.RightHanded },   // 武器技
            { ActorActionType.StartDefence,	     WeaponArmType.RightHanded },   // 开始防御
            { ActorActionType.Defence,		     WeaponArmType.RightHanded },   // 防御
            { ActorActionType.DefenceBeAttack,   WeaponArmType.RightHanded },   // 攻击被防住
            { ActorActionType.Dodge,             WeaponArmType.RightHanded },   // 回避
            { ActorActionType.Freeze,            WeaponArmType.None        },   // 眠定
            { ActorActionType.Dying,             WeaponArmType.RightHanded },   // 濒死
            { ActorActionType.Dead,			     WeaponArmType.None        },   // 死亡
            { ActorActionType.Flee,			     WeaponArmType.None        },   // 逃跑等到
            { ActorActionType.Win,			     WeaponArmType.RightHanded },   // 胜利

            // NPC,怪物动作
            { ActorActionType.NpcStand1,         WeaponArmType.None },    // 站立1
            { ActorActionType.NpcStand2,         WeaponArmType.None },    // 站立2
            { ActorActionType.NpcWalk,	         WeaponArmType.None },    // 行走
            { ActorActionType.NpcAttack,         WeaponArmType.None },    // 攻击
            { ActorActionType.NpcBeAttacked,     WeaponArmType.None },    // 攻击
            { ActorActionType.NpcMagic1,         WeaponArmType.None },    // 仙术1
            { ActorActionType.NpcMagic2,         WeaponArmType.None },    // 仙术2
            { ActorActionType.NpcMagic3,         WeaponArmType.None },    // 仙术3
            { ActorActionType.NpcRun,            WeaponArmType.None },    // 跑,用走代替
        };
        #elif PAL3A        
        public static readonly Dictionary<ActorActionType, WeaponArmType> ActionNameToWeaponArmTypeMap = new()
        {
            // 基本动作
            { ActorActionType.Stand,             WeaponArmType.None },   // 站立
            { ActorActionType.Walk,              WeaponArmType.None },   // 行走
            { ActorActionType.Run,               WeaponArmType.None },   // 奔跑
            { ActorActionType.Back,              WeaponArmType.None },   // 后退
            { ActorActionType.Jump,              WeaponArmType.None },   // 起跳
            { ActorActionType.Climb,             WeaponArmType.None },   // 向上攀爬
            { ActorActionType.ClimbDown,         WeaponArmType.None },   // 向下攀爬
            { ActorActionType.Push,              WeaponArmType.None },   // 推
            { ActorActionType.Skill,             WeaponArmType.None },   // 技能
            { ActorActionType.Tire,              WeaponArmType.None },   // 疲倦
            { ActorActionType.Seat,              WeaponArmType.None },   // 坐
            { ActorActionType.Check,             WeaponArmType.None },   // 调查
            { ActorActionType.Sleep,             WeaponArmType.None },   // 睡觉
            { ActorActionType.Shake,             WeaponArmType.None },   // 摇头
            { ActorActionType.Nod,               WeaponArmType.None },   // 点头
            { ActorActionType.Wave,              WeaponArmType.None },   // 摆手

            // 剧情动作
            { ActorActionType.Threaten,          WeaponArmType.None },   // 威胁
            { ActorActionType.Shocked,           WeaponArmType.None },   // 惊吓
            { ActorActionType.BendOver,          WeaponArmType.None },   // 下蹲
            { ActorActionType.SeatUp,            WeaponArmType.None },   // 坐起
            { ActorActionType.Point,             WeaponArmType.None },   // 指点
            { ActorActionType.Kick,              WeaponArmType.None },   // 踢
            { ActorActionType.Salute,            WeaponArmType.None },   // 行礼
            { ActorActionType.Tease,             WeaponArmType.None },   // 挑逗
            { ActorActionType.Snuggle,           WeaponArmType.None },   // 依偎
            { ActorActionType.Whining,           WeaponArmType.None },   // 撒娇
            { ActorActionType.Anger,             WeaponArmType.None },   // 发怒
            { ActorActionType.Dose,              WeaponArmType.None },   // 服药
            { ActorActionType.J13,               WeaponArmType.None },   // 得意

            // 战斗动作
            { ActorActionType.Attack1,		     WeaponArmType.RightHanded },   // 攻击1
            { ActorActionType.Attack2,		     WeaponArmType.RightHanded },   // 攻击2
            { ActorActionType.AttackMove,	     WeaponArmType.RightHanded },   // 攻击移动
            { ActorActionType.PreAttack,         WeaponArmType.RightHanded },   // 备战
            { ActorActionType.BeAttack,		     WeaponArmType.None        },   // 被攻击
            { ActorActionType.MagicWait,         WeaponArmType.RightHanded },   // 仙术预备
            { ActorActionType.Magic,             WeaponArmType.RightHanded },   // 仙术
            { ActorActionType.Skill1,		     WeaponArmType.RightHanded },   // 特技1
            { ActorActionType.Skill2,		     WeaponArmType.RightHanded },   // 特技2
            { ActorActionType.UseItemRecover,    WeaponArmType.None        },   // 恢复物品
            { ActorActionType.UseItemAttack,     WeaponArmType.RightHanded },   // 攻击物品
            { ActorActionType.WeaponSkill,	     WeaponArmType.RightHanded },   // 武器技
            { ActorActionType.StartDefence,	     WeaponArmType.RightHanded },   // 开始防御
            { ActorActionType.Defence,		     WeaponArmType.RightHanded },   // 防御
            { ActorActionType.DefenceBeAttack,   WeaponArmType.RightHanded },   // 攻击被防住
            { ActorActionType.Dodge,             WeaponArmType.RightHanded },   // 回避
            { ActorActionType.Freeze,            WeaponArmType.None        },   // 眠定
            { ActorActionType.Dying,             WeaponArmType.None        },   // 濒死
            { ActorActionType.Dead,			     WeaponArmType.None        },   // 死亡
            { ActorActionType.Flee,			     WeaponArmType.None        },   // 逃跑等到
            { ActorActionType.Win,			     WeaponArmType.RightHanded },   // 胜利

            // NPC,怪物动作
            { ActorActionType.NpcStand1,         WeaponArmType.None },    // 站立1
            { ActorActionType.NpcStand2,         WeaponArmType.None },    // 站立2
            { ActorActionType.NpcWalk,	         WeaponArmType.None },    // 行走
            { ActorActionType.NpcAttack,         WeaponArmType.None },    // 攻击
            { ActorActionType.NpcBeAttacked,     WeaponArmType.None },    // 攻击
            { ActorActionType.NpcMagic1,         WeaponArmType.None },    // 仙术1
            { ActorActionType.NpcMagic2,         WeaponArmType.None },    // 仙术2
            { ActorActionType.NpcMagic3,         WeaponArmType.None },    // 仙术3
            { ActorActionType.NpcRun,            WeaponArmType.None },    // 跑,用走代替
            
            // 仙剑三外传新增动作
            { ActorActionType.LevelUp,          WeaponArmType.None },    // 升级
            { ActorActionType.Transform,        WeaponArmType.None },    // 变身
            { ActorActionType.Attack21,         WeaponArmType.None },    // 扔八卦的第一段动作
            { ActorActionType.Attack22,	        WeaponArmType.None },    // 扔八卦的第二段动作
            { ActorActionType.Attack23,	        WeaponArmType.None },    // 扔八卦的第三段动作
        };
        #endif
        
        #if PAL3
        public static readonly HashSet<ActorActionType> ActionWithoutShadow = new()
        {
            ActorActionType.Sleep,
            ActorActionType.SeatUp,
            ActorActionType.Dead,
            ActorActionType.Climb,
            ActorActionType.ClimbDown,
            ActorActionType.Jump,
        };
        #elif PAL3A
        public static readonly HashSet<ActorActionType> ActionWithoutShadow = new()
        {
            ActorActionType.Dead,
            ActorActionType.Climb,
            ActorActionType.ClimbDown,
            ActorActionType.Jump,
        };
        #endif
    }
}