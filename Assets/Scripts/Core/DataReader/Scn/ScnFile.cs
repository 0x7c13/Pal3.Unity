// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Core.DataReader.Scn
{
    using System;
    using Contracts;
    using GameBox;
    using Newtonsoft.Json;
    using UnityEngine;

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
        public SceneType SceneType;     // 0 普通场景, 1 战斗场景, 2 室内场景
        public int LightMap;            // 0 日景灯光, 1 夜景灯光, -1 无灯光？(比如M01)
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

        public bool Is(string cityName, string sceneName)
        {
            return IsCity(cityName) && IsScene(sceneName);
        }

        public bool IsCity(string cityName)
        {
            return string.Equals(CityName, cityName, StringComparison.OrdinalIgnoreCase);
        }

        public bool IsScene(string sceneName)
        {
            return string.Equals(SceneName, sceneName, StringComparison.OrdinalIgnoreCase);
        }

        public override string ToString()
        {
            return $"City: {CityName.ToLower()}, " +
                   $"Scene: {SceneName.ToLower()}, " +
                   $"Model: {Model.ToLower()}, " +
                   $"Type: {SceneType}, " +
                   $"LightMap: {LightMap}, " +
                   $"SkyBox: {SkyBox}";
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
        public ActorType Type;

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
        public ActorBehaviourType InitBehaviour;

        // 互动脚本
        public uint ScriptId;

        // 初始Y坐标,只有在 InitBehaviour == Hold
        // 的时候 GameBoxYPosition 和 InitAction 才有用
        public float GameBoxYPosition; // 原GameBox引擎下的Y坐标

        // chars[16] 初始动作
        public string InitAction;

        // uint[3] 怪物种类ID
        public uint[] MonsterIds;
        public byte NumberOfMonsters;
        public byte MonsterCanRespawn;

        // Empty byte between the memory addresses to align the data in memory.
        // NumberOfMonsters and MonsterCanRespawn take 2 bytes in memory, thus we need to
        // add 2 more bytes here to align the data in memory (4 bytes).
        internal byte[] PaddingBytes;

        public ScnPath Path;

        // 对话开始时： 0: 可转向, 1: 不可转向
        public uint NoTurn;

        public uint LoopAction;

        // 移动速度
        public uint GameBoxMoveSpeed;

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
        public float GameBoxYRotation;

        // 触发范围(TileMap区域)
        public GameBoxRect TileMapTriggerRect;

        // 场景Object类型
        public SceneObjectType Type;

        // 是否保存状态
        public byte SaveState;

        // 在哪个地层
        public byte LayerIndex;

        // 五灵属性,0~5代表,用于大宝箱和三外中具有五灵属性的机关
        public SceneObjectElementType ElementType;

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
        public uint NotUsed; // always 0
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
        public ushort LinkedObjectGroupId;        // 同时触发的关联编号 （如阳名百纳二的机关浮板，神魔结界的三相桥）
        #endif

        public uint ScriptId;

        public ScnPath Path;

        // 触发结果
        public ushort LinkedObjectId;      // 触发其它机关的编号;

        // 与场景的某个开关相关
        public string DependentSceneName;  // char[32]
        public byte DependentObjectId;

        public Bounds Bounds;
        public float GameBoxXRotation;     // 绕X轴旋转
        #if PAL3A
        public float GameBoxZRotation;     // 绕Z轴旋转
        #endif
        public string SfxName;             // 音效文件名

        public uint EffectModelType;

        public uint ScriptActivated;
        public uint ScriptMoved;

        #if PAL3A
        public uint CanOnlyBeTriggeredOnce;
        #endif

        public uint[] Reserved;            // PAL3: 52 DWORDs PAL3A: 44 DWORDs
    }

    /// <summary>
    /// SCN (.scn) file model
    /// </summary>
    public sealed class ScnFile
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