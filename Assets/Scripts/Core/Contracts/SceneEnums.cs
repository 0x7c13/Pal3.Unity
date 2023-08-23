// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Core.Contracts
{
    [System.Serializable]
    public enum FloorType
    {
        Default = 0,      // 不可跳
        Jumpable,         // 可跳过
        Soil,             // 土
        Mud,              // 泥
        River,            // 水道(船状态)
        Snow,             // 雪
        Wet,              // 水浸
        Grass,            // 草
        Wood,             // 竹木
        Plant,            // 植物
        Wetland,          // 沼泽
        Sand,             // 沙
        Metal,            // 金属
        Rock1,            // 熔岩
        Rock2,            // 岩石基底地面
        Rock3,            // 人工石地面
        Brick,            // 砖
        DevilWorld1,      // 异空间A
        DevilWorld2,      // 异空间B
        DevilWorld3,      // 异空间C
        Other,            // 其它
        Ice,              // 冰
        Blood,            // 血池
        Water,            // 水域
    }

    [System.Serializable]
    public enum SceneObjectElementType
    {
        Water   = 0,    // 水
        Fire    = 1,    // 火
        Wind    = 2,    // 风
        Thunder = 3,    // 雷
        Earth   = 4,    // 土
    }

    [System.Serializable]
    public enum ActorType
    {
        StoryNpc =      0,   // 情节NPC
        HotelManager =  1,   // 客店老板
        CombatNpc =     2,   // 战斗NPC
        Dealer =        3,   // 小贩(职业动作)
        Soldier =       4,   // 官兵
        MainActor =  0xFF,   // 主角
    }

    [System.Serializable]
    public enum ActorBehaviourType
    {
        None = 0,
        Wander,
        PathFollow,
        Hold,
        Seek,
        Pursuit,
        Evasion,
        PathLayer,
        MoveBack,
        NpcPathFollow,
        PetFollow,
        PetFly,
        Key,
    }

    [System.Serializable]
    public enum SceneType
    {
        OutDoor = 0,  // 情节关城市村镇
        InDoor,       // 情节关建筑内部
        Maze,         // 迷宫场景
    }

    [System.Serializable]
    public enum SceneObjectType
    {
        AutoTrigger                   =  0,   // 自动触发点
        Shakeable                     =  1,   // 碰到后摇晃的物品
        #if PAL3
        Collidable                    =  2,   // 碰到后翻倒的物品(仙三独有)
        #elif PAL3A
        ElementSwitch                 =  2,   // 五灵开关 (可触发其他机关,外传独有)
        #endif
        Arrow                         =  3,   // 飞箭类 (霹雳堂总舵飞剑)
        FallableWeapon                =  4,   // 落下的伤害物体 (冰棱)
        MovableCarrier                =  5,   // 移动浮板 (固定速度和路线往复移动,可载人)
        Trap                          =  6,   // 陷阱 (调用脚本切换场景)
        RotatingBridge                =  7,   // 旋传石梁 (仙三独有)
        Teapot                        =  8,   // 茶壶 (外传独有)
        PreciseTrigger                =  9,   // 精确触发点
        WindBlower                    = 10,   // 风口
        Pushable                      = 11,   // 推箱子(仙三独有)
        SpecialSwitch                 = 12,   // 主角特技机关(仙三独有)
        Impulsive                     = 13,   // 冲撞类机关(仙三独有)
        Door                          = 14,   // 门，场景切换点
        Climbable                     = 15,   // 藤蔓或梯子 (可爬物)
        InvestigationTrigger          = 16,   // 调查触发物体
        StaticOrAnimated              = 17,   // 死物体或者随机动画播放(通用参数[0]:对应的地面是否可通过)
        Billboard                     = 18,   // 公告板
        SuspensionBridge              = 19,   // 吊桥 (仙三独有)
        VirtualInvestigationTrigger   = 20,   // 虚拟BOX调查触发
        EyeBall                       = 21,   // 锁妖塔:眼球(仙三独有)
        LiftingPlatform               = 22,   // 锁妖塔:升降铁链或平台
                                              // 通用参数[0]:为升起后的高度,需要用其他开关间接触发
                                              // 通用参数[1]:升起后到高层
                                              // 通用参数[2]:平台上的其他物件编号
                                              // 通用参数[3]:初始为可通过?)
        Switch                        = 23,   // 普通开关 (可触发其他机关,仙三独有)
        Collectable                   = 24,   // 可以拣起的道具 (通用参数[0]:得到物品的数据库ID)
        General                       = 25,   // 普通物品
        RareChest                     = 26,   // 迷宫大宝箱
        #if PAL3
        WishPool                      = 27,   // 许愿池(仙三独有)
        #elif PAL3A
        ToggleSwitch                  = 27,   // 普通开关的变种(可触发其他机关,外传独有)
        #endif
        ColdWeapon                    = 28,   // (M24)剑或锤
        GravitySwitch                 = 29,   // 重力机关（乌龟,仙三独有）
        ElevatorDoor                  = 30,   // 升降机关门(仙三独有)
        WaterSurfaceRoadBlocker       = 31,   // 水面机关/路障(仙三独有)
        PiranhaFlower                 = 32,   // 食人花
        PedalSwitch                   = 33,   // 踏板开关
        SwordBridge                   = 34,   // 剑桥（触发后向前伸,仙三独有）
        SlideWay                      = 35,   // 滑道
        FallableObstacle              = 36,   // 坠落的障碍物(仙三独有)
        DivineTreeFlower              = 37,   // 神树花(仙三独有)
        DivineTreePortal              = 38,   // 神树传送点
        Elevator                      = 39,   // 神魔之井电梯(仙三独有)
        ElevatorPedal                 = 40,   // 上下传送板(仙三独有)
        Chest                         = 41,   // 小宝箱
        SavingPoint                   = 42,   // 存盘点
        SceneSfx                      = 43,   // 场景音效
        JumpableArea                  = 44,   // 可跳跃区域

        // PAL3A new scene objects
        // 以下所有均为仙三外传中新增的场景物件类型
        PaperweightDesk               = 47,   // 镇尺桌
        Paperweight                   = 48,   // 镇尺
        RoadElevatorOrBlocker         = 49,   // 可升降路面或者是阻挡物
        RetractableBlocker            = 51,   // 可收回路障
        RotatingWall                  = 52,   // T形旋转石门
        WaterSurface                  = 53,   // 升降水面
        ThreePhaseBridge              = 54,   // 三相桥
        ThreePhaseSwitch              = 55,   // 三相开关
        MushroomBridge                = 56,   // 蘑菇桥
        SectorBridge                  = 59,   // 扇型桥
    }
}