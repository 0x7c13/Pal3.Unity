// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Core.DataReader.Scn
{
    using System;
    using GameBox;
    using Newtonsoft.Json;
    using UnityEngine;

    [System.Serializable]
    public enum ScnActorKind
    {
        StoryNpc =      0,   // 情节NPC
        HotelManager =  1,   // 客店老板
        CombatNpc =     2,   // 战斗NPC
        Dealer =        3,   // 小贩(职业动作)
        Soldier =       4,   // 官兵
        MainActor =  0xFF,   // 主角
    }

    [System.Serializable]
    public enum ScnActorBehaviour
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
    public enum ScnSceneType
    {
        StoryA = 0,  // 情节关城市村镇
        StoryB,      // 情节关建筑内部
        Maze,        // 战斗（迷宫）场景
    }

    [System.Serializable]
    public enum ScnSceneObjectType
    {
        AutoTrigger                   =  0,   // 自动触发点
        Shakeable                     =  1,   // 碰到后摇晃的物品
        Collidable                    =  2,   // 碰到后翻倒的物品
        Arrow                         =  3,   // 飞箭类 (霹雳堂总舵飞剑)
        FallableWeapon                =  4,   // 落下的伤害物体 (冰棱)
        MovableCarrier                =  5,   // 移动浮板 (固定速度和路线往复移动,可载人)
        Trap                          =  6,   // 陷阱 (调用脚本切换场景)
        RotatingStoneBeam             =  7,   // 旋传石梁 (一字型)
        RotatingWall                  =  8,   // 旋转墙 (折尺型)
        PreciseTrigger                =  9,   // 精确触发点
        WindBlower                    = 10,   // 风口
        Pushable                      = 11,   // 推箱子
        SpecialMechanism              = 12,   // 主角特技机关
        ImpulsiveMechanism            = 13,   // 冲撞类机关
        Door                          = 14,   // 门，场景切换点
        Climbable                     = 15,   // 藤蔓或梯子 (可爬物)
        InvestigationTriggerObject    = 16,   // 调查触发物体
        StaticOrAnimated              = 17,   // 死物体或者随机动画播放(通用参数[0]:对应的地面是否可通过)
        Billboard                     = 18,   // 公告板
        SuspensionBridge              = 19,   // 吊桥 (壁山)
        VirtualInvestigationTrigger   = 20,   // 虚拟BOX调查触发
        EyeBall                       = 21,   // 锁妖塔:眼球
        LiftingMechanism              = 22,   // 锁妖塔:升降铁链或平台
                                              // 通用参数[0]:为升起后的高度,需要用其他开关间接触发
                                              // 通用参数[1]:升起后到高层
                                              // 通用参数[2]:平台上的其他物件编号
                                              // 通用参数[3]:初始为可通过?)
        Switch                        = 23,   // 开关 (可触发其他机关)
        Collectable                   = 24,   // 可以拣起的道具 (通用参数[0]:得到物品的数据库ID)
        General                       = 25,   // 普通物品
        RareChest                     = 26,   // 迷宫大宝箱
        WishPool                      = 27,   // 许愿池
        ColdWeapon                    = 28,   // (M24)剑或锤
        GravityTrigger                = 29,   // 重力机关（乌龟）
        ElevatorDoor                  = 30,   // 升降机关门
        WaterSurfaceMechanism         = 31,   // 水面机关
        PiranhaFlower                 = 32,   // 食人花
        PedalSwitch                   = 33,   // 踏板开关
        SwordBridge                   = 34,   // (m24)剑桥（触发后向前伸）
        SlideWay                      = 35,   // 滑道
        FallableObstacle              = 36,   // 坠落的障碍物
        DivineTreeFlower              = 37,   // (神树)花
        DivineTreePortal              = 38,   // (神树)传送点
        Elevator                      = 39,   // (神魔之井)电梯
        ElevatorPedal                 = 40,   // 上下传送板
        Chest                         = 41,   // 小宝箱
        SavingPoint                   = 42,   // 存盘点
        SceneSfx                      = 43,   // 场景音效
        JumpableArea                  = 44,   // 可跳跃区域

        // PAL3A new scene objects
        UnknownObj47                  = 47,   // PAL3A m19 8
        UnknownObj48                  = 48,   // PAL3A m19 8
        UnknownObj49                  = 49,   // PAL3A m05 3, m06 2, m17 2, m17 3, m17 4, m18 1, m18 2
        UnknownObj51                  = 51,   // PAL3A m10 2
        UnknownObj52                  = 52,   // PAL3A m01 1
        UnknownObj53                  = 53,   // PAL3A m17 1, m17 2, m17 3, m17 4
        UnknownObj54                  = 54,   // PAL3A m13 1, m13 2, m13 3, m13 4
        UnknownObj55                  = 55,   // PAL3A m13 1, m13 2, m13 3, m13 4
        UnknownObj56                  = 56,   // PAL3A m15 1
        UnknownObj59                  = 59,   // PAL3A m15 2
    }

    // SCN (.scn) file header
    public struct ScnHeader
    {
        public string Magic;             // 4 bytes
        public ushort Version;
        public ushort NumberOfNpc;
        public uint NpcDataOffset;
        public ushort NumberOfObjects;
        public uint ObjectDataOffset;
    }

    [System.Serializable]
    public struct ScnPath
    {
        public int NumberOfWaypoints;
        public Vector3[] GameBoxWaypoints;   // 原GameBox引擎坐标系下的路径点（X, Y, Z）数组。固定数组长度为16.
    }

    public struct ScnSceneInfo
    {
        public string CityName;         // char[32] 关卡/区块名称
        public string SceneName;        // char[32] 场景名称
        public string Model;            // char[32] 模型名称
        public ScnSceneType SceneType;
        public int LightMap;            // 0日景灯光, 1夜景灯光, -1无灯光？(比如M01)
        public uint SkyBox;
        public uint[] Reserved;         // 6 DWORDs

        /// <summary>
        /// Check if two scene share the same model.
        /// </summary>
        /// <param name="sceneInfo">ScnSceneInfo</param>
        /// <returns>True if two scene share the same model</returns>
        public bool ModelEquals(ScnSceneInfo sceneInfo)
        {
            #if PAL3
            return string.Equals(CityName, sceneInfo.CityName, StringComparison.OrdinalIgnoreCase) &&
                   string.Equals(Model, sceneInfo.Model, StringComparison.OrdinalIgnoreCase);

            #elif PAL3A
            var modelAName = Model;
            if (modelAName.EndsWith("y", StringComparison.OrdinalIgnoreCase))
            {
                modelAName = modelAName[..^1];
            }
            var modelBName = sceneInfo.Model;
            if (modelBName.EndsWith("y", StringComparison.OrdinalIgnoreCase))
            {
                modelBName = modelBName[..^1];

            }
            return string.Equals(CityName, sceneInfo.CityName, StringComparison.OrdinalIgnoreCase) &&
                   string.Equals(modelAName, modelBName, StringComparison.OrdinalIgnoreCase);
            #endif
        }

        public override string ToString()
        {
            return $"City: {CityName.ToLower()}, Scene: {SceneName.ToLower()}, " +
                   $"Model: {Model.ToLower()}, Type: {SceneType}, LightMap: {LightMap}, SkyBox: {SkyBox}";
        }
    }

    [System.Serializable]
    public class ScnNpcInfo
    {
        // 在一关中唯一的编号 (NPC不能跨场景)
        public byte Id;

        // 0: 情节NPC
        // 1: 客店老板
        // 2: 战斗NPC
        // 3: 小贩(职业动作)
        // 4: 官兵
        // 255(0xFF): 主角
        public ScnActorKind Kind;

        // char[32] Npc名字
        public string Name;

        // char[34] 贴图编号 （无用）
        public string Texture;

        // 人物初始朝向
        public float FacingDirection;

        // 初始地层
        public int LayerIndex;

        // 原GameBox引擎下的初始位置（X，Z）
        public float GameBoxXPosition;
        public float GameBoxZPosition;

        // 初始是否可见
        public int InitActive;

        // 初始行为
        public ScnActorBehaviour InitBehaviour;

        // 互动脚本
        public uint ScriptId;

        // 初始Y坐标,只有在 InitBehaviour == Hold
        // 的时候 GameBoxYPosition 和 InitAction 才有用
        public float GameBoxYPosition; // 原GameBox引擎下的Y坐标

        // chars[16] 初始动作
        public string InitAction;

        // uint[3] 怪物种类ID
        public uint[] MonsterId;
        public ushort MonsterNumber;
        public ushort MonsterRepeat;

        public ScnPath Path;

        public uint NoTurn;
        public uint LoopAction;

        // 移动速度
        public uint Speed;

        [JsonIgnore]
        public uint[] Reserved; // 29 DWORDs
    }

    [System.Serializable]
    public struct ScnObjectInfo
    {
        // 一关中唯一的编号
        public byte Id;

        // 初始是否激活 (不激活相当于没这个物体)
        public byte InitActive;

        // 可触发次数 (0表示不可触发，1表示只能触发一次（收集类道具），0xFF表示无限触发)
        public byte Times;

        // 开关状态 (0代表关, 1代表开)
        public byte SwitchState;

        // char[32] 物品名称,对应美术模型,带扩展名
        public string Name;

        // 触发类型，1地砖自动触发，2模型调查触发
        public byte TriggerType;

        // 是否产生阻挡
        public byte IsNonBlocking;

        // 原GameBox引擎下的模型位置
        public Vector3 GameBoxPosition;

        // 渲染模型的时候转动多少角度(绕Y轴)
        public float YRotation;

        // 触发范围(TileMap区域)
        public GameBoxRect TileMapTriggerRect;

        // 场景Object类型
        public ScnSceneObjectType Type;

        // 是否保存状态
        public byte SaveState;

        // 在哪个地层
        public byte LayerIndex;

        // 五灵属性,0~5代表,用于大宝箱
        public byte WuLing;

        /*
            通用参数 int[6]:
            13 冲撞
                [0]:落脚点TILE_X
                [1]:落脚点TILE_Z
            17 死物体或者随机动画播放
                [0]:对应的地面是否可通过
            18 公告板
                [0]:公告板ID
            22 锁妖塔:升降铁链或平台
                [0]:为升起后的高度,需要用其他开关间接触发
                [1]:升起后到高层
                [2]:平台上的其他物件编号
                [3]:初始为可通过?)
            24 以拣起的道具
                [0]:得到物品的数据库ID
        */

        public int[] Parameters;

        #if PAL3A
        public uint Unknown1; // always 0
        #endif

        // 触发条件
        public byte RequireSpecialAction;          // 对应特殊行走技能,0xFF为无此条件
        public ushort RequireItem;                 // 需要物品,0xFF为无此条件
        public ushort RequireMoney;                // 需要金钱值
        public ushort RequireLevel;                // 需要等级数
        public ushort RequireAttackValue;          // 需要武力值 (如果无此限制,则设成最小值)
        public byte RequireAllMechanismsSolved;    // 场景中所有需检测的机关都为开
        public string FailedMessage;               // char[16] 失败提示字符串名称

        #if PAL3A
        public uint Unknown2;
        #endif

        public uint ScriptId;

        public ScnPath Path;

        // 触发结果
        public ushort LinkedObjectId;      // 触发其它机关的编号;

        // 与场景的某个开关相关
        public string DependentSceneName;  // char[32]
        public byte DependentObjectId;

        public Bounds Bounds;
        public float XRotation;            // 绕X轴旋转
        #if PAL3A
        public float ZRotation;            // 绕Z轴旋转
        #endif
        public string SfxName;             // 音效文件名

        public uint EffectModelType;

        public uint ScriptActivated;
        public uint ScriptMoved;

        #if PAL3A
        public uint Unknown3;
        #endif

        public uint[] Reserved;            // PAL3: 52 DWORDs PAL3A: 44 DWORDs
    }

    /// <summary>
    /// SCN (.scn) file model
    /// </summary>
    public class ScnFile
    {
        public ScnSceneInfo SceneInfo { get; }
        public ScnNpcInfo[] NpcInfos { get; }
        public ScnObjectInfo[] ObjectInfos { get; }

        public ScnFile(ScnSceneInfo sceneInfo, ScnNpcInfo[] npcInfos, ScnObjectInfo[] objectInfos)
        {
            SceneInfo = sceneInfo;
            NpcInfos = npcInfos;
            ObjectInfos = objectInfos;
        }
    }
}