// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Dev
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using Command;
    using Command.InternalCommands;
    using Command.SceCommands;
    using IngameDebugConsole;
    using Input;
    using MetaData;
    using Player;
    using Scene;
    using Script;
    using State;
    using TMPro;
    using UI;
    using UnityEngine;
    using UnityEngine.EventSystems;
    using UnityEngine.InputSystem;
    using UnityEngine.InputSystem.DualShock;
    using UnityEngine.UI;

    public sealed class StorySelector : MonoBehaviour,
        ICommandExecutor<ToggleStorySelectorRequest>,
        ICommandExecutor<GameSwitchToMainMenuCommand>,
        ICommandExecutor<ScenePostLoadingNotification>
    {
        private InputManager _inputManager;
        private EventSystem _eventSystem;
        private PlayerInputActions _playerInputActions;
        private ScriptManager _scriptManager;
        private TeamManager _teamManager;
        private GameStateManager _gameStateManager;
        private SceneManager _sceneManager;
        private SaveManager _saveManager;
        private InformationManager _informationManager;
        private CanvasGroup _storySelectorCanvas;
        private GameObject _storySelectorButtonPrefab;

        private readonly List<string> _deferredExecutionCommands = new();

        private readonly List<GameObject> _selectionButtons = new();

        private readonly Dictionary<string, string> _storySelections = new()
        {
            {"关闭", ""},
            {"退出游戏", ""},
            {"继续游戏", ""},
            {"保存当前游戏进度", ""},
            {"新的游戏", ""},
            #if PAL3
            {"永安当-去客房找赵文昌", @"
                ScriptVarSetValue -32768 10202
                SceneLoad q01 y
                ActorActivate -1 1
                ActorEnablePlayerControl -1
                PlayerEnableInput 1
                ActorSetNavLayer -1 0
                ActorSetTilePosition -1 77 175
                TeamAddOrRemoveActor 0 1
                CameraFadeIn"},
            {"渝州-去唐家堡找雪见", @"
                ScriptVarSetValue -32768 10401
                SceneLoad q01 b
                ActorActivate -1 1
                ActorEnablePlayerControl -1
                PlayerEnableInput 1
                ActorSetNavLayer -1 0
                ActorSetTilePosition -1 195 212
                TeamAddOrRemoveActor 0 1
                CameraFadeIn"},
            {"渝州-第一次见重楼前", @"
                ScriptVarSetValue -32768 10800
                SceneLoad q01 ba
                ActorActivate -1 1
                ActorEnablePlayerControl -1
                PlayerEnableInput 1
                ActorSetNavLayer -1 0
                ActorSetTilePosition -1 190 230
                TeamAddOrRemoveActor 0 1
                CameraFadeIn"},
            {"渝州西南-入住客栈过夜", @"
                ScriptVarSetValue -32768 11101
                SceneLoad q01 xa
                ActorActivate -1 1
                ActorEnablePlayerControl -1
                PlayerEnableInput 1
                ActorSetNavLayer -1 0
                ActorSetTilePosition -1 215 47
                TeamAddOrRemoveActor 0 1
                TeamAddOrRemoveActor 1 1
                CameraFadeIn"},
            {"宾化-寻找雪见", @"
                ScriptVarSetValue -32768 20200
                SceneLoad q02 q02
                ActorActivate -1 1
                ActorEnablePlayerControl -1
                PlayerEnableInput 1
                ActorSetNavLayer -1 0
                ActorSetTilePosition -1 70 209
                TeamAddOrRemoveActor 0 1
                CameraFadeIn"},
            {"船上醒来", @"
                ScriptVarSetValue -32768 20700
                SceneLoad q17 n04
                ActorActivate -1 1
                ActorEnablePlayerControl -1
                PlayerEnableInput 1
                ActorSetNavLayer -1 0
                ActorSetTilePosition -1 11 8
                TeamAddOrRemoveActor 0 1
                TeamAddOrRemoveActor 1 1
                CameraFadeIn"},
            {"镇江-第一次进入", @"
                ScriptVarSetValue -32768 20901
                SceneLoad q03 q03
                ActorActivate -1 1
                ActorEnablePlayerControl -1
                PlayerEnableInput 1
                ActorSetNavLayer -1 0
                ActorSetTilePosition -1 325 165
                TeamAddOrRemoveActor 0 1
                TeamAddOrRemoveActor 1 1
                TeamAddOrRemoveActor 4 1
                TeamAddOrRemoveActor 3 1
                CameraFadeIn"},
            {"蓬莱御剑堂-初遇邪剑仙", @"
                ScriptVarSetValue -32768 21300
                SceneLoad q04 q04
                ActorActivate -1 1
                ActorEnablePlayerControl -1
                PlayerEnableInput 1
                ActorSetNavLayer -1 0
                ActorSetTilePosition -1 57 47
                TeamAddOrRemoveActor 0 1
                TeamAddOrRemoveActor 1 1
                TeamAddOrRemoveActor 3 1
                CameraFadeIn"},
            {"蓬莱-离开前往唐家堡", @"
                ScriptVarSetValue -32768 21500
                SceneLoad M06 1
                ActorActivate -1 1
                ActorEnablePlayerControl -1
                PlayerEnableInput 1
                ActorSetNavLayer -1 0
                ActorSetTilePosition -1 22 99
                TeamAddOrRemoveActor 0 1
                TeamAddOrRemoveActor 1 1
                TeamAddOrRemoveActor 3 1
                CameraFadeIn"},
            {"渝州-从后门返回家中", @"
                ScriptVarSetValue -32768 30500
                SceneLoad q01 b
                ActorActivate -1 1
                ActorEnablePlayerControl -1
                PlayerEnableInput 1
                ActorSetNavLayer -1 0
                ActorSetTilePosition -1 204 78
                TeamAddOrRemoveActor 0 1
                CameraFadeIn"},
            {"渝州-第一次见龙葵前", @"
                ScriptVarSetValue -32768 30800
                SceneLoad q01 ba
                ActorActivate -1 1
                ActorEnablePlayerControl -1
                PlayerEnableInput 1
                ActorSetNavLayer -1 0
                ActorSetTilePosition -1 277 293
                TeamAddOrRemoveActor 0 1
                CameraFadeIn"},
            {"渝州西南-找到雪见", @"
                ScriptVarSetValue -32768 31100
                SceneLoad q01 x
                ActorActivate -1 1
                ActorEnablePlayerControl -1
                PlayerEnableInput 1
                ActorSetNavLayer -1 0
                ActorSetTilePosition -1 147 45
                TeamAddOrRemoveActor 0 1
                TeamAddOrRemoveActor 2 1
                CameraFadeIn"},
            {"德阳-第一次进入", @"
                ScriptVarSetValue -32768 40100
                SceneLoad q06 q06
                ActorActivate -1 1
                ActorEnablePlayerControl -1
                PlayerEnableInput 1
                ActorSetNavLayer -1 0
                ActorSetTilePosition -1 395 48
                TeamAddOrRemoveActor 0 1
                TeamAddOrRemoveActor 2 1
                TeamAddOrRemoveActor 1 1
                CameraFadeIn"},
            {"安宁村-第一次进入", @"
                ScriptVarSetValue -32768 40400
                SceneLoad Q07 Q07a
                ActorActivate -1 1
                ActorEnablePlayerControl -1
                PlayerEnableInput 1
                ActorSetNavLayer -1 0
                ActorSetTilePosition -1 87 240
                TeamAddOrRemoveActor 0 1
                TeamAddOrRemoveActor 2 1
                TeamAddOrRemoveActor 1 1
                TeamAddOrRemoveActor 3 1
                CameraFadeIn"},
            {"古藤林-遇万玉枝", @"
                ScriptVarSetValue -32768 40900
                SceneLoad m10 1
                ActorActivate -1 1
                ActorEnablePlayerControl -1
                PlayerEnableInput 1
                ActorSetNavLayer -1 0
                ActorSetTilePosition -1 231 38
                TeamAddOrRemoveActor 0 1
                TeamAddOrRemoveActor 2 1
                TeamAddOrRemoveActor 1 1
                TeamAddOrRemoveActor 3 1
                CameraFadeIn"},
            {"安宁村-返回万玉枝家", @"
                ScriptVarSetValue -32768 50100
                SceneLoad Q07 Q07
                ActorActivate -1 1
                ActorEnablePlayerControl -1
                PlayerEnableInput 1
                ActorSetNavLayer -1 0
                ActorSetTilePosition -1 224 148
                TeamAddOrRemoveActor 0 1
                TeamAddOrRemoveActor 2 1
                TeamAddOrRemoveActor 1 1
                TeamAddOrRemoveActor 3 1
                CameraFadeIn"},
            {"蜀山故道-初入蜀山前", @"
                ScriptVarSetValue -32768 50300
                SceneLoad M11 2
                ActorActivate -1 1
                ActorEnablePlayerControl -1
                PlayerEnableInput 1
                ActorSetNavLayer -1 0
                ActorSetTilePosition -1 188 112
                TeamAddOrRemoveActor 0 1
                TeamAddOrRemoveActor 2 1
                TeamAddOrRemoveActor 1 1
                TeamAddOrRemoveActor 3 1
                CameraFadeIn"},
            {"唐家堡-从蜀山返回", @"
                ScriptVarSetValue -32768 60100
                SceneLoad q05 q05
                ActorActivate -1 1
                ActorEnablePlayerControl -1
                PlayerEnableInput 1
                ActorSetNavLayer -1 0
                ActorSetTilePosition -1 181 21
                TeamAddOrRemoveActor 0 1
                TeamAddOrRemoveActor 2 1
                TeamAddOrRemoveActor 1 1
                TeamAddOrRemoveActor 4 1
                CameraFadeIn"},
            {"雷州-第一次进入", @"
                ScriptVarSetValue -32768 70100
                SceneLoad q09 c
                ActorActivate -1 1
                ActorEnablePlayerControl -1
                PlayerEnableInput 1
                ActorSetNavLayer -1 0
                ActorSetTilePosition -1 204 277
                TeamAddOrRemoveActor 0 1
                TeamAddOrRemoveActor 4 1
                TeamAddOrRemoveActor 2 1
                TeamAddOrRemoveActor 1 1
                CameraFadeIn"},
            {"雷州-去临风楼找雪见", @"
                ScriptVarSetValue -32768 70800
                SceneLoad q09 c
                ActorActivate -1 1
                ActorEnablePlayerControl -1
                PlayerEnableInput 1
                ActorSetNavLayer -1 0
                ActorSetTilePosition -1 86 146
                TeamAddOrRemoveActor 0 1
                TeamAddOrRemoveActor 4 1
                TeamAddOrRemoveActor 2 1
                CameraFadeIn"},
            {"刺史府-去神魔之井前", @"
                ScriptVarSetValue -32768 71402
                SceneLoad q09 f
                ActorActivate -1 1
                ActorEnablePlayerControl -1
                PlayerEnableInput 1
                ActorSetNavLayer -1 0
                ActorSetTilePosition -1 30 146
                TeamAddOrRemoveActor 0 1
                TeamAddOrRemoveActor 4 1
                TeamAddOrRemoveActor 2 1
                TeamAddOrRemoveActor 1 1
                CameraFadeIn"},
            {"神界天门-第一次进入", @"
                ScriptVarSetValue -32768 72100
                SceneLoad q10 q10
                ActorActivate -1 1
                ActorEnablePlayerControl -1
                PlayerEnableInput 1
                ActorSetNavLayer -1 0
                ActorSetTilePosition -1 96 257
                TeamAddOrRemoveActor 0 1
                TeamAddOrRemoveActor 4 1
                TeamAddOrRemoveActor 2 1
                TeamAddOrRemoveActor 1 1
                CameraFadeIn"},
            {"蜀山-从神界返回", @"
                ScriptVarSetValue -32768 90200
                SceneLoad q08 q08p
                ActorActivate -1 1
                ActorEnablePlayerControl -1
                PlayerEnableInput 1
                ActorSetNavLayer -1 1
                ActorSetTilePosition -1 305 298
                TeamAddOrRemoveActor 0 1
                TeamAddOrRemoveActor 4 1
                TeamAddOrRemoveActor 2 1
                TeamAddOrRemoveActor 1 1
                CameraFadeIn"},
            {"锁妖塔-第一次进入", @"
                ScriptVarSetValue -32768 90500
                SceneLoad m17 1
                ActorActivate -1 1
                ActorEnablePlayerControl -1
                PlayerEnableInput 1
                ActorSetNavLayer -1 0
                ActorSetTilePosition -1 20 32
                TeamAddOrRemoveActor 0 1
                TeamAddOrRemoveActor 3 1
                TeamAddOrRemoveActor 2 1
                TeamAddOrRemoveActor 1 1
                CameraFadeIn"},
            {"锁妖塔-返回蜀山", @"
                ScriptVarSetValue -32768 90501
                SceneLoad m17 10
                ActorActivate -1 1
                ActorEnablePlayerControl -1
                PlayerEnableInput 1
                ActorSetNavLayer -1 0
                ActorSetTilePosition -1 64 9
                TeamAddOrRemoveActor 0 1
                TeamAddOrRemoveActor 3 1
                TeamAddOrRemoveActor 2 1
                TeamAddOrRemoveActor 1 1
                SceneActivateObject 2 1
                CameraFadeIn"},
            {"蛮州-第一次进入", @"
                ScriptVarSetValue -32768 100400
                SceneLoad q11 q11
                ActorActivate -1 1
                ActorEnablePlayerControl -1
                PlayerEnableInput 1
                ActorSetNavLayer -1 0
                ActorSetTilePosition -1 146 383
                TeamAddOrRemoveActor 0 1
                TeamAddOrRemoveActor 3 1
                TeamAddOrRemoveActor 2 1
                CameraFadeIn"},
            {"古城镇-第一次进入", @"
                ScriptVarSetValue -32768 110300
                SceneLoad q12 q12
                ActorActivate -1 1
                ActorEnablePlayerControl -1
                PlayerEnableInput 1
                ActorSetNavLayer -1 0
                ActorSetTilePosition -1 47 42
                TeamAddOrRemoveActor 0 1
                TeamAddOrRemoveActor 3 1
                TeamAddOrRemoveActor 2 1
                CameraFadeIn"},
            {"古城镇-上祭坛之前\n(雪见最高好感)", @"
                ScriptVarSetValue -32768 110600
                SceneLoad q12 q12
                ActorActivate -1 1
                ActorEnablePlayerControl -1
                PlayerEnableInput 1
                ActorSetNavLayer -1 0
                ActorSetTilePosition -1 214 247
                TeamAddOrRemoveActor 0 1
                TeamAddOrRemoveActor 3 1
                TeamAddOrRemoveActor 2 1
                FavorAdd 1 50
                CameraFadeIn"},
            {"古城镇-上祭坛之前\n(龙葵最高好感)", @"
                ScriptVarSetValue -32768 110600
                SceneLoad q12 q12
                ActorActivate -1 1
                ActorEnablePlayerControl -1
                PlayerEnableInput 1
                ActorSetNavLayer -1 0
                ActorSetTilePosition -1 214 247
                TeamAddOrRemoveActor 0 1
                TeamAddOrRemoveActor 3 1
                TeamAddOrRemoveActor 2 1
                FavorAdd 2 50
                CameraFadeIn"},
            {"蜀山-从古城镇返回", @"
                ScriptVarSetValue -32768 111000
                SceneLoad q08 q08p
                ActorActivate -1 1
                ActorEnablePlayerControl -1
                PlayerEnableInput 1
                ActorSetNavLayer -1 1
                ActorSetTilePosition -1 156 389
                TeamAddOrRemoveActor 0 1
                TeamAddOrRemoveActor 2 1
                TeamAddOrRemoveActor 3 1
                BigMapEnableRegion 0 2
                BigMapEnableRegion 2 2
                BigMapEnableRegion 3 2
                BigMapEnableRegion 5 2
                BigMapEnableRegion 6 1
                BigMapEnableRegion 7 1
                BigMapEnableRegion 8 2
                BigMapEnableRegion 9 2
                BigMapEnableRegion 11 2
                BigMapEnableRegion 12 2
                BigMapEnableRegion 17 2
                BigMapEnableRegion 18 2
                BigMapEnableRegion 19 2
                BigMapEnableRegion 20 2
                BigMapEnableRegion 21 1
                BigMapEnableRegion 22 2
                BigMapEnableRegion 23 2
                BigMapEnableRegion 24 2
                BigMapEnableRegion 26 1
                BigMapEnableRegion 27 1
                BigMapEnableRegion 28 2
                BigMapEnableRegion 29 2
                BigMapEnableRegion 30 2
                BigMapEnableRegion 31 2
                BigMapEnableRegion 32 2
                BigMapEnableRegion 36 2
                BigMapEnableRegion 37 2
                CameraFadeIn"},
            {"酆都-第一次进入", @"
                ScriptVarSetValue -32768 120100
                SceneLoad q13 q13
                ActorActivate -1 1
                ActorEnablePlayerControl -1
                PlayerEnableInput 1
                ActorSetNavLayer -1 0
                ActorSetTilePosition -1 323 40
                TeamAddOrRemoveActor 0 1
                TeamAddOrRemoveActor 2 1
                TeamAddOrRemoveActor 3 1
                BigMapEnableRegion 0 2
                BigMapEnableRegion 2 2
                BigMapEnableRegion 3 2
                BigMapEnableRegion 5 2
                BigMapEnableRegion 6 1
                BigMapEnableRegion 7 1
                BigMapEnableRegion 8 2
                BigMapEnableRegion 9 2
                BigMapEnableRegion 11 2
                BigMapEnableRegion 12 2
                BigMapEnableRegion 17 2
                BigMapEnableRegion 18 2
                BigMapEnableRegion 19 2
                BigMapEnableRegion 20 2
                BigMapEnableRegion 21 1
                BigMapEnableRegion 22 2
                BigMapEnableRegion 23 2
                BigMapEnableRegion 24 2
                BigMapEnableRegion 26 1
                BigMapEnableRegion 27 1
                BigMapEnableRegion 28 2
                BigMapEnableRegion 29 2
                BigMapEnableRegion 30 2
                BigMapEnableRegion 31 2
                BigMapEnableRegion 32 2
                BigMapEnableRegion 36 2
                BigMapEnableRegion 37 2
                BigMapEnableRegion 10 2
                CameraFadeIn"},
            {"鬼界外围-第一次进入", @"
                ScriptVarSetValue -32768 120400
                SceneLoad q14 q14
                ActorActivate -1 1
                ActorEnablePlayerControl -1
                PlayerEnableInput 1
                ActorSetNavLayer -1 0
                ActorSetTilePosition -1 123 274
                TeamAddOrRemoveActor 0 1
                TeamAddOrRemoveActor 2 1
                BigMapEnableRegion 0 2
                BigMapEnableRegion 2 2
                BigMapEnableRegion 3 2
                BigMapEnableRegion 5 2
                BigMapEnableRegion 6 1
                BigMapEnableRegion 7 1
                BigMapEnableRegion 8 2
                BigMapEnableRegion 9 2
                BigMapEnableRegion 11 2
                BigMapEnableRegion 12 2
                BigMapEnableRegion 17 2
                BigMapEnableRegion 18 2
                BigMapEnableRegion 19 2
                BigMapEnableRegion 20 2
                BigMapEnableRegion 21 1
                BigMapEnableRegion 22 2
                BigMapEnableRegion 23 2
                BigMapEnableRegion 24 2
                BigMapEnableRegion 26 1
                BigMapEnableRegion 27 1
                BigMapEnableRegion 28 2
                BigMapEnableRegion 29 2
                BigMapEnableRegion 30 2
                BigMapEnableRegion 31 2
                BigMapEnableRegion 32 2
                BigMapEnableRegion 36 2
                BigMapEnableRegion 37 2
                BigMapEnableRegion 10 2
                BigMapEnableRegion 13 1
                CameraFadeIn"},
            {"鬼界外围-从地狱返回", @"
                ScriptVarSetValue -32768 121100
                SceneLoad q14 q14
                ActorActivate -1 1
                ActorEnablePlayerControl -1
                PlayerEnableInput 1
                ActorSetNavLayer -1 0
                ActorSetTilePosition -1 25 71
                TeamAddOrRemoveActor 0 1
                TeamAddOrRemoveActor 2 1
                TeamAddOrRemoveActor 1 1
                TeamAddOrRemoveActor 3 1
                BigMapEnableRegion 0 2
                BigMapEnableRegion 2 2
                BigMapEnableRegion 3 2
                BigMapEnableRegion 5 2
                BigMapEnableRegion 6 1
                BigMapEnableRegion 7 1
                BigMapEnableRegion 8 2
                BigMapEnableRegion 9 2
                BigMapEnableRegion 11 2
                BigMapEnableRegion 12 2
                BigMapEnableRegion 17 2
                BigMapEnableRegion 18 2
                BigMapEnableRegion 19 2
                BigMapEnableRegion 20 2
                BigMapEnableRegion 21 1
                BigMapEnableRegion 22 2
                BigMapEnableRegion 23 2
                BigMapEnableRegion 24 2
                BigMapEnableRegion 26 1
                BigMapEnableRegion 27 1
                BigMapEnableRegion 28 2
                BigMapEnableRegion 29 2
                BigMapEnableRegion 30 2
                BigMapEnableRegion 31 2
                BigMapEnableRegion 32 2
                BigMapEnableRegion 36 2
                BigMapEnableRegion 37 2
                BigMapEnableRegion 10 2
                BigMapEnableRegion 13 1
                BigMapEnableRegion 25 1
                BigMapEnableRegion 15 1
                CameraFadeIn"},
            {"蜀山-从黄泉路返回后", @"
                ScriptVarSetValue -32768 121200
                SceneLoad q08 q08p
                ActorActivate -1 1
                ActorEnablePlayerControl -1
                PlayerEnableInput 1
                ActorSetNavLayer -1 0
                ActorSetTilePosition -1 17 23
                TeamAddOrRemoveActor 0 1
                TeamAddOrRemoveActor 2 1
                TeamAddOrRemoveActor 1 1
                TeamAddOrRemoveActor 3 1
                BigMapEnableRegion 0 2
                BigMapEnableRegion 2 2
                BigMapEnableRegion 3 2
                BigMapEnableRegion 5 2
                BigMapEnableRegion 6 1
                BigMapEnableRegion 7 1
                BigMapEnableRegion 8 2
                BigMapEnableRegion 9 2
                BigMapEnableRegion 11 2
                BigMapEnableRegion 12 2
                BigMapEnableRegion 17 2
                BigMapEnableRegion 18 2
                BigMapEnableRegion 19 2
                BigMapEnableRegion 20 2
                BigMapEnableRegion 21 1
                BigMapEnableRegion 22 2
                BigMapEnableRegion 23 2
                BigMapEnableRegion 24 2
                BigMapEnableRegion 26 1
                BigMapEnableRegion 27 1
                BigMapEnableRegion 28 2
                BigMapEnableRegion 29 2
                BigMapEnableRegion 30 2
                BigMapEnableRegion 31 2
                BigMapEnableRegion 32 2
                BigMapEnableRegion 36 2
                BigMapEnableRegion 37 2
                BigMapEnableRegion 10 2
                BigMapEnableRegion 13 1
                BigMapEnableRegion 25 1
                BigMapEnableRegion 15 1
                CameraFadeIn"},
            {"雪岭镇-第一次进入", @"
                ScriptVarSetValue -32768 130200
                SceneLoad q15 q15
                ActorActivate -1 1
                ActorEnablePlayerControl -1
                PlayerEnableInput 1
                ActorSetNavLayer -1 0
                ActorSetTilePosition -1 225 22
                TeamAddOrRemoveActor 0 1
                TeamAddOrRemoveActor 2 1
                TeamAddOrRemoveActor 1 1
                TeamAddOrRemoveActor 3 1
                BigMapEnableRegion 0 2
                BigMapEnableRegion 2 2
                BigMapEnableRegion 3 2
                BigMapEnableRegion 5 2
                BigMapEnableRegion 6 1
                BigMapEnableRegion 7 1
                BigMapEnableRegion 8 2
                BigMapEnableRegion 9 2
                BigMapEnableRegion 11 2
                BigMapEnableRegion 12 2
                BigMapEnableRegion 17 2
                BigMapEnableRegion 18 2
                BigMapEnableRegion 19 2
                BigMapEnableRegion 20 2
                BigMapEnableRegion 21 1
                BigMapEnableRegion 22 2
                BigMapEnableRegion 23 2
                BigMapEnableRegion 24 2
                BigMapEnableRegion 26 1
                BigMapEnableRegion 27 1
                BigMapEnableRegion 28 2
                BigMapEnableRegion 29 2
                BigMapEnableRegion 30 2
                BigMapEnableRegion 31 2
                BigMapEnableRegion 32 2
                BigMapEnableRegion 36 2
                BigMapEnableRegion 37 2
                BigMapEnableRegion 10 2
                BigMapEnableRegion 13 1
                BigMapEnableRegion 25 1
                BigMapEnableRegion 15 1
                BigMapEnableRegion 35 2
                CameraFadeIn"},
            {"蜀山-从雪岭镇返回", @"
                ScriptVarSetValue -32768 131001
                SceneLoad q08 q08p
                ActorActivate -1 1
                ActorEnablePlayerControl -1
                PlayerEnableInput 1
                ActorSetNavLayer -1 1
                ActorSetTilePosition -1 156 387
                TeamAddOrRemoveActor 0 1
                TeamAddOrRemoveActor 1 1
                TeamAddOrRemoveActor 2 1
                TeamAddOrRemoveActor 3 1
                BigMapEnableRegion 0 2
                BigMapEnableRegion 2 2
                BigMapEnableRegion 3 2
                BigMapEnableRegion 5 2
                BigMapEnableRegion 6 1
                BigMapEnableRegion 7 1
                BigMapEnableRegion 8 2
                BigMapEnableRegion 9 2
                BigMapEnableRegion 11 2
                BigMapEnableRegion 12 2
                BigMapEnableRegion 17 2
                BigMapEnableRegion 18 2
                BigMapEnableRegion 19 2
                BigMapEnableRegion 20 2
                BigMapEnableRegion 21 1
                BigMapEnableRegion 22 2
                BigMapEnableRegion 23 2
                BigMapEnableRegion 24 2
                BigMapEnableRegion 26 1
                BigMapEnableRegion 27 1
                BigMapEnableRegion 28 2
                BigMapEnableRegion 29 2
                BigMapEnableRegion 30 2
                BigMapEnableRegion 31 2
                BigMapEnableRegion 32 2
                BigMapEnableRegion 36 2
                BigMapEnableRegion 37 2
                BigMapEnableRegion 10 2
                BigMapEnableRegion 13 1
                BigMapEnableRegion 25 1
                BigMapEnableRegion 15 1
                BigMapEnableRegion 35 2
                BigMapEnableRegion 4 2
                CameraFadeIn"},
            {"安溪-第一次进入", @"
                ScriptVarSetValue -32768 131100
                SceneLoad q16 q16
                ActorActivate -1 1
                ActorEnablePlayerControl -1
                PlayerEnableInput 1
                ActorSetNavLayer -1 0
                ActorSetTilePosition -1 371 81
                TeamAddOrRemoveActor 0 1
                TeamAddOrRemoveActor 1 1
                TeamAddOrRemoveActor 2 1
                TeamAddOrRemoveActor 3 1
                BigMapEnableRegion 0 2
                BigMapEnableRegion 2 2
                BigMapEnableRegion 3 2
                BigMapEnableRegion 5 2
                BigMapEnableRegion 6 1
                BigMapEnableRegion 7 1
                BigMapEnableRegion 8 2
                BigMapEnableRegion 9 2
                BigMapEnableRegion 11 2
                BigMapEnableRegion 12 2
                BigMapEnableRegion 17 2
                BigMapEnableRegion 18 2
                BigMapEnableRegion 19 2
                BigMapEnableRegion 20 2
                BigMapEnableRegion 21 1
                BigMapEnableRegion 22 2
                BigMapEnableRegion 23 2
                BigMapEnableRegion 24 2
                BigMapEnableRegion 26 1
                BigMapEnableRegion 27 1
                BigMapEnableRegion 28 2
                BigMapEnableRegion 29 2
                BigMapEnableRegion 30 2
                BigMapEnableRegion 31 2
                BigMapEnableRegion 32 2
                BigMapEnableRegion 36 2
                BigMapEnableRegion 37 2
                BigMapEnableRegion 10 2
                BigMapEnableRegion 13 1
                BigMapEnableRegion 25 1
                BigMapEnableRegion 15 1
                BigMapEnableRegion 35 2
                BigMapEnableRegion 4 2
                BigMapEnableRegion 1 2
                CameraFadeIn"},
            {"安溪-从海底城返回", @"
                ScriptVarSetValue -32768 140500
                SceneLoad q16 q16
                ActorActivate -1 1
                ActorEnablePlayerControl -1
                PlayerEnableInput 1
                ActorSetNavLayer -1 0
                ActorSetTilePosition -1 371 81
                TeamAddOrRemoveActor 0 1
                TeamAddOrRemoveActor 1 1
                TeamAddOrRemoveActor 2 1
                TeamAddOrRemoveActor 3 1
                BigMapEnableRegion 0 2
                BigMapEnableRegion 2 2
                BigMapEnableRegion 3 2
                BigMapEnableRegion 5 2
                BigMapEnableRegion 6 1
                BigMapEnableRegion 7 1
                BigMapEnableRegion 8 2
                BigMapEnableRegion 9 2
                BigMapEnableRegion 11 2
                BigMapEnableRegion 12 2
                BigMapEnableRegion 17 2
                BigMapEnableRegion 18 2
                BigMapEnableRegion 19 2
                BigMapEnableRegion 20 2
                BigMapEnableRegion 21 1
                BigMapEnableRegion 22 2
                BigMapEnableRegion 23 2
                BigMapEnableRegion 24 2
                BigMapEnableRegion 26 1
                BigMapEnableRegion 27 1
                BigMapEnableRegion 28 2
                BigMapEnableRegion 29 2
                BigMapEnableRegion 30 2
                BigMapEnableRegion 31 2
                BigMapEnableRegion 32 2
                BigMapEnableRegion 36 2
                BigMapEnableRegion 37 2
                BigMapEnableRegion 10 2
                BigMapEnableRegion 13 1
                BigMapEnableRegion 25 1
                BigMapEnableRegion 15 1
                BigMapEnableRegion 35 2
                BigMapEnableRegion 4 2
                BigMapEnableRegion 1 2
                BigMapEnableRegion 14 2
                CameraFadeIn"},
            {"双剑选择名场面\n(雪见好感度最高)", @"
                ScriptVarSetValue -32768 140900
                SceneLoad m24 6
                ActorActivate -1 1
                ActorEnablePlayerControl -1
                PlayerEnableInput 1
                ActorSetNavLayer -1 0
                ActorSetTilePosition -1 174 107
                TeamAddOrRemoveActor 0 1
                TeamAddOrRemoveActor 1 1
                TeamAddOrRemoveActor 2 1
                TeamAddOrRemoveActor 3 1
                BigMapEnableRegion 0 2
                BigMapEnableRegion 2 2
                BigMapEnableRegion 3 2
                BigMapEnableRegion 5 2
                BigMapEnableRegion 6 1
                BigMapEnableRegion 7 1
                BigMapEnableRegion 8 2
                BigMapEnableRegion 9 2
                BigMapEnableRegion 11 2
                BigMapEnableRegion 12 2
                BigMapEnableRegion 17 2
                BigMapEnableRegion 18 2
                BigMapEnableRegion 19 2
                BigMapEnableRegion 20 2
                BigMapEnableRegion 21 1
                BigMapEnableRegion 22 2
                BigMapEnableRegion 23 2
                BigMapEnableRegion 24 2
                BigMapEnableRegion 26 1
                BigMapEnableRegion 27 1
                BigMapEnableRegion 28 2
                BigMapEnableRegion 29 2
                BigMapEnableRegion 30 2
                BigMapEnableRegion 31 2
                BigMapEnableRegion 32 2
                BigMapEnableRegion 36 2
                BigMapEnableRegion 37 2
                BigMapEnableRegion 10 2
                BigMapEnableRegion 13 1
                BigMapEnableRegion 25 1
                BigMapEnableRegion 15 1
                BigMapEnableRegion 35 2
                BigMapEnableRegion 4 2
                BigMapEnableRegion 1 2
                BigMapEnableRegion 14 2
                BigMapEnableRegion 16 2
                FavorAdd 1 50
                CameraFadeIn"},
            {"双剑选择名场面\n(龙葵好感度最高)", @"
                ScriptVarSetValue -32768 140900
                SceneLoad m24 6
                ActorActivate -1 1
                ActorEnablePlayerControl -1
                PlayerEnableInput 1
                ActorSetNavLayer -1 0
                ActorSetTilePosition -1 174 107
                TeamAddOrRemoveActor 0 1
                TeamAddOrRemoveActor 1 1
                TeamAddOrRemoveActor 2 1
                TeamAddOrRemoveActor 3 1
                BigMapEnableRegion 0 2
                BigMapEnableRegion 2 2
                BigMapEnableRegion 3 2
                BigMapEnableRegion 5 2
                BigMapEnableRegion 6 1
                BigMapEnableRegion 7 1
                BigMapEnableRegion 8 2
                BigMapEnableRegion 9 2
                BigMapEnableRegion 11 2
                BigMapEnableRegion 12 2
                BigMapEnableRegion 17 2
                BigMapEnableRegion 18 2
                BigMapEnableRegion 19 2
                BigMapEnableRegion 20 2
                BigMapEnableRegion 21 1
                BigMapEnableRegion 22 2
                BigMapEnableRegion 23 2
                BigMapEnableRegion 24 2
                BigMapEnableRegion 26 1
                BigMapEnableRegion 27 1
                BigMapEnableRegion 28 2
                BigMapEnableRegion 29 2
                BigMapEnableRegion 30 2
                BigMapEnableRegion 31 2
                BigMapEnableRegion 32 2
                BigMapEnableRegion 36 2
                BigMapEnableRegion 37 2
                BigMapEnableRegion 10 2
                BigMapEnableRegion 13 1
                BigMapEnableRegion 25 1
                BigMapEnableRegion 15 1
                BigMapEnableRegion 35 2
                BigMapEnableRegion 4 2
                BigMapEnableRegion 1 2
                BigMapEnableRegion 14 2
                BigMapEnableRegion 16 2
                FavorAdd 2 50
                CameraFadeIn"},
            {"双剑选择名场面\n(紫萱好感度最高)", @"
                ScriptVarSetValue -32768 140900
                SceneLoad m24 6
                ActorActivate -1 1
                ActorEnablePlayerControl -1
                PlayerEnableInput 1
                ActorSetNavLayer -1 0
                ActorSetTilePosition -1 174 107
                TeamAddOrRemoveActor 0 1
                TeamAddOrRemoveActor 1 1
                TeamAddOrRemoveActor 2 1
                TeamAddOrRemoveActor 3 1
                BigMapEnableRegion 0 2
                BigMapEnableRegion 2 2
                BigMapEnableRegion 3 2
                BigMapEnableRegion 5 2
                BigMapEnableRegion 6 1
                BigMapEnableRegion 7 1
                BigMapEnableRegion 8 2
                BigMapEnableRegion 9 2
                BigMapEnableRegion 11 2
                BigMapEnableRegion 12 2
                BigMapEnableRegion 17 2
                BigMapEnableRegion 18 2
                BigMapEnableRegion 19 2
                BigMapEnableRegion 20 2
                BigMapEnableRegion 21 1
                BigMapEnableRegion 22 2
                BigMapEnableRegion 23 2
                BigMapEnableRegion 24 2
                BigMapEnableRegion 26 1
                BigMapEnableRegion 27 1
                BigMapEnableRegion 28 2
                BigMapEnableRegion 29 2
                BigMapEnableRegion 30 2
                BigMapEnableRegion 31 2
                BigMapEnableRegion 32 2
                BigMapEnableRegion 36 2
                BigMapEnableRegion 37 2
                BigMapEnableRegion 10 2
                BigMapEnableRegion 13 1
                BigMapEnableRegion 25 1
                BigMapEnableRegion 15 1
                BigMapEnableRegion 35 2
                BigMapEnableRegion 4 2
                BigMapEnableRegion 1 2
                BigMapEnableRegion 14 2
                BigMapEnableRegion 16 2
                FavorAdd 3 50
                FavorAdd 1 10
                CameraFadeIn"},
            {"双剑选择名场面\n(花楹好感度最高)", @"
                ScriptVarSetValue -32768 140900
                SceneLoad m24 6
                ActorActivate -1 1
                ActorEnablePlayerControl -1
                PlayerEnableInput 1
                ActorSetNavLayer -1 0
                ActorSetTilePosition -1 174 107
                TeamAddOrRemoveActor 0 1
                TeamAddOrRemoveActor 1 1
                TeamAddOrRemoveActor 2 1
                TeamAddOrRemoveActor 3 1
                BigMapEnableRegion 0 2
                BigMapEnableRegion 2 2
                BigMapEnableRegion 3 2
                BigMapEnableRegion 5 2
                BigMapEnableRegion 6 1
                BigMapEnableRegion 7 1
                BigMapEnableRegion 8 2
                BigMapEnableRegion 9 2
                BigMapEnableRegion 11 2
                BigMapEnableRegion 12 2
                BigMapEnableRegion 17 2
                BigMapEnableRegion 18 2
                BigMapEnableRegion 19 2
                BigMapEnableRegion 20 2
                BigMapEnableRegion 21 1
                BigMapEnableRegion 22 2
                BigMapEnableRegion 23 2
                BigMapEnableRegion 24 2
                BigMapEnableRegion 26 1
                BigMapEnableRegion 27 1
                BigMapEnableRegion 28 2
                BigMapEnableRegion 29 2
                BigMapEnableRegion 30 2
                BigMapEnableRegion 31 2
                BigMapEnableRegion 32 2
                BigMapEnableRegion 36 2
                BigMapEnableRegion 37 2
                BigMapEnableRegion 10 2
                BigMapEnableRegion 13 1
                BigMapEnableRegion 25 1
                BigMapEnableRegion 15 1
                BigMapEnableRegion 35 2
                BigMapEnableRegion 4 2
                BigMapEnableRegion 1 2
                BigMapEnableRegion 14 2
                BigMapEnableRegion 16 2
                FavorAdd 5 50
                FavorAdd 2 10
                CameraFadeIn"},
            #elif PAL3A
            {"与温慧相遇后去酒馆", @"
                ScriptVarSetValue -32768 10500
                SceneLoad Q01 Q01
                ActorActivate -1 1
                ActorEnablePlayerControl -1
                PlayerEnableInput 1
                ActorSetNavLayer -1 0
                ActorSetTilePosition -1 96 169
                TeamAddOrRemoveActor 0 1
                CameraFadeIn"},
            {"唐家堡-登云麓", @"
                ScriptVarSetValue -32768 10800
                SceneLoad Q01 Q01
                ActorActivate -1 1
                ActorEnablePlayerControl -1
                PlayerEnableInput 1
                ActorSetNavLayer -1 0
                ActorSetTilePosition -1 190 20
                TeamAddOrRemoveActor 0 1
                CameraFadeIn"},
            {"蜀山-找酒剑仙", @"
                ScriptVarSetValue -32768 20200
                SceneLoad q02 xH
                ActorActivate -1 1
                ActorEnablePlayerControl -1
                PlayerEnableInput 1
                ActorSetNavLayer -1 0
                ActorSetTilePosition -1 332 185
                TeamAddOrRemoveActor 0 1
                CameraFadeIn"},
            {"蜀山-第一次回家", @"
                ScriptVarSetValue -32768 20800
                SceneLoad q02 qH
                ActorActivate -1 1
                ActorEnablePlayerControl -1
                PlayerEnableInput 1
                ActorSetNavLayer -1 0
                ActorSetTilePosition -1 91 114
                TeamAddOrRemoveActor 0 1
                CameraFadeIn"},
            {"蜀山-绿萝嶂", @"
                ScriptVarSetValue -32768 30101
                SceneLoad Q02 HH
                ActorActivate -1 1
                ActorEnablePlayerControl -1
                PlayerEnableInput 1
                ActorSetNavLayer -1 0
                ActorSetTilePosition -1 342 137
                TeamAddOrRemoveActor 0 1
                TeamAddOrRemoveActor 1 1
                CameraFadeIn"},
            {"绿萝山-蜀山", @"
                ScriptVarSetValue -32768 30900
                SceneLoad q03 q03
                ActorActivate -1 1
                ActorEnablePlayerControl -1
                PlayerEnableInput 1
                ActorSetNavLayer -1 0
                ActorSetTilePosition -1 94 179
                TeamAddOrRemoveActor 2 1
                TeamAddOrRemoveActor 1 1
                TeamAddOrRemoveActor 0 1
                BigMapEnableRegion 0 0
                BigMapEnableRegion 1 0
                BigMapEnableRegion 2 2
                BigMapEnableRegion 4 2
                BigMapEnableRegion 5 2
                CameraFadeIn"},
            {"地脉门户-少阳三焦", @"
                ScriptVarSetValue -32768 40200
                SceneLoad m02 2
                ActorActivate -1 1
                ActorEnablePlayerControl -1
                PlayerEnableInput 1
                ActorSetNavLayer -1 0
                ActorSetTilePosition -1 29 66
                TeamAddOrRemoveActor 1 1
                TeamAddOrRemoveActor 0 1
                BigMapEnableRegion 0 2
                BigMapEnableRegion 1 2
                BigMapEnableRegion 2 2
                BigMapEnableRegion 4 2
                BigMapEnableRegion 5 2
                CameraFadeIn"},
            {"第一次进入里蜀山", @"
                ScriptVarSetValue -32768 41100
                SceneLoad q04 wb
                ActorActivate -1 1
                ActorEnablePlayerControl -1
                PlayerEnableInput 1
                ActorSetNavLayer -1 0
                ActorSetTilePosition -1 321 102
                TeamAddOrRemoveActor 2 1
                TeamAddOrRemoveActor 1 1
                TeamAddOrRemoveActor 0 1
                BigMapEnableRegion 0 2
                BigMapEnableRegion 1 2
                BigMapEnableRegion 2 2
                BigMapEnableRegion 4 2
                BigMapEnableRegion 5 2
                BigMapEnableRegion 6 1
                CameraFadeIn"},
            {"打完狸猫回废屋休息", @"
                ScriptVarSetValue -32768 41600
                SceneLoad q04 wn
                ActorActivate -1 1
                ActorEnablePlayerControl -1
                PlayerEnableInput 1
                ActorSetNavLayer -1 0
                ActorSetTilePosition -1 183 290
                TeamAddOrRemoveActor 2 1
                TeamAddOrRemoveActor 1 1
                TeamAddOrRemoveActor 0 1
                BigMapEnableRegion 0 2
                BigMapEnableRegion 1 2
                BigMapEnableRegion 2 2
                BigMapEnableRegion 4 2
                BigMapEnableRegion 5 2
                BigMapEnableRegion 6 1
                CameraFadeIn"},
            {"里蜀山外城南-厥阴心包", @"
                ScriptVarSetValue -32768 50200
                SceneLoad q04 wn
                ActorActivate -1 1
                ActorEnablePlayerControl -1
                PlayerEnableInput 1
                ActorSetNavLayer -1 0
                ActorSetTilePosition -1 346 180
                TeamAddOrRemoveActor 2 1
                TeamAddOrRemoveActor 1 1
                TeamAddOrRemoveActor 0 1
                BigMapEnableRegion 0 2
                BigMapEnableRegion 1 2
                BigMapEnableRegion 2 2
                BigMapEnableRegion 4 2
                BigMapEnableRegion 5 2
                BigMapEnableRegion 6 1
                CameraFadeIn"},
            {"厥阴心包-胜州", @"
                ScriptVarSetValue -32768 50500
                SceneLoad M05 4
                ActorActivate -1 1
                ActorEnablePlayerControl -1
                PlayerEnableInput 1
                ActorSetNavLayer -1 0
                ActorSetTilePosition -1 127 15
                TeamAddOrRemoveActor 2 1
                TeamAddOrRemoveActor 1 1
                TeamAddOrRemoveActor 0 1
                BigMapEnableRegion 0 2
                BigMapEnableRegion 1 2
                BigMapEnableRegion 2 2
                BigMapEnableRegion 4 2
                BigMapEnableRegion 5 2
                BigMapEnableRegion 6 1
                CameraFadeIn"},
            {"胜州祭坛欢庆盛典", @"
                ScriptVarSetValue -32768 51100
                SceneLoad q05 q05
                ActorActivate -1 1
                ActorEnablePlayerControl -1
                PlayerEnableInput 1
                ActorSetNavLayer -1 0
                ActorSetTilePosition -1 417 177
                TeamAddOrRemoveActor 2 1
                TeamAddOrRemoveActor 1 1
                TeamAddOrRemoveActor 0 1
                TeamAddOrRemoveActor 4 1
                BigMapEnableRegion 0 2
                BigMapEnableRegion 1 2
                BigMapEnableRegion 2 2
                BigMapEnableRegion 4 2
                BigMapEnableRegion 5 2
                BigMapEnableRegion 6 1
                BigMapEnableRegion 7 2
                BigMapEnableRegion 8 2
                CameraFadeIn"},
            {"胜州-蜀山", @"
                ScriptVarSetValue -32768 60100
                SceneLoad q05 q05
                ActorActivate -1 1
                ActorEnablePlayerControl -1
                PlayerEnableInput 1
                ActorSetNavLayer -1 0
                ActorSetTilePosition -1 391 283
                TeamAddOrRemoveActor 2 1
                TeamAddOrRemoveActor 1 1
                TeamAddOrRemoveActor 0 1
                TeamAddOrRemoveActor 4 1
                BigMapEnableRegion 0 2
                BigMapEnableRegion 1 2
                BigMapEnableRegion 2 2
                BigMapEnableRegion 4 2
                BigMapEnableRegion 5 2
                BigMapEnableRegion 6 1
                BigMapEnableRegion 7 2
                BigMapEnableRegion 8 2
                CameraFadeIn"},
            {"蜀山-深夜去经库", @"
                ScriptVarSetValue -32768 60500
                SceneLoad q02 qY
                ActorActivate -1 1
                ActorEnablePlayerControl -1
                PlayerEnableInput 1
                ActorSetNavLayer -1 0
                ActorSetTilePosition -1 335 157
                TeamAddOrRemoveActor 0 1
                TeamAddOrRemoveActor 1 1
                TeamAddOrRemoveActor 4 1
                BigMapEnableRegion 0 2
                BigMapEnableRegion 1 2
                BigMapEnableRegion 2 2
                BigMapEnableRegion 4 2
                BigMapEnableRegion 5 2
                BigMapEnableRegion 6 1
                BigMapEnableRegion 7 2
                BigMapEnableRegion 8 2
                CameraFadeIn"},
            {"地脉门户大厅-阳名百纳", @"
                ScriptVarSetValue -32768 70100
                SceneLoad m02 2
                ActorActivate -1 1
                ActorEnablePlayerControl -1
                PlayerEnableInput 1
                ActorSetNavLayer -1 0
                ActorSetTilePosition -1 55 18
                TeamAddOrRemoveActor 0 1
                TeamAddOrRemoveActor 1 1
                BigMapEnableRegion 0 2
                BigMapEnableRegion 1 2
                BigMapEnableRegion 2 2
                BigMapEnableRegion 4 2
                BigMapEnableRegion 5 2
                BigMapEnableRegion 6 1
                BigMapEnableRegion 7 2
                BigMapEnableRegion 8 2
                CameraFadeIn"},
            {"血濡回魂-初入京城", @"
                ScriptVarSetValue -32768 70700
                SceneLoad q06 q06
                ActorActivate -1 1
                ActorEnablePlayerControl -1
                PlayerEnableInput 1
                ActorSetNavLayer -1 0
                ActorSetTilePosition -1 13 192
                TeamAddOrRemoveActor 0 1
                BigMapEnableRegion 0 2
                BigMapEnableRegion 1 2
                BigMapEnableRegion 2 2
                BigMapEnableRegion 4 2
                BigMapEnableRegion 5 2
                BigMapEnableRegion 6 1
                BigMapEnableRegion 7 2
                BigMapEnableRegion 8 2
                BigMapEnableRegion 9 1
                CameraFadeIn"},
            {"血濡回魂-蜀山深夜", @"
                ScriptVarSetValue -32768 71501
                SceneLoad Q11 Q11
                ActorActivate -1 1
                ActorEnablePlayerControl -1
                PlayerEnableInput 1
                ActorSetNavLayer -1 1
                ActorSetTilePosition -1 82 221
                TeamAddOrRemoveActor 0 1
                BigMapEnableRegion 0 2
                BigMapEnableRegion 1 2
                BigMapEnableRegion 2 2
                BigMapEnableRegion 4 2
                BigMapEnableRegion 5 2
                BigMapEnableRegion 6 1
                BigMapEnableRegion 7 2
                BigMapEnableRegion 8 2
                BigMapEnableRegion 9 1
                CameraFadeIn"},
            {"血濡回魂-梦醒", @"
                ScriptVarSetValue -32768 72001
                SceneLoad m07 3
                ActorActivate -1 1
                ActorEnablePlayerControl -1
                PlayerEnableInput 1
                ActorSetNavLayer -1 0
                ActorSetTilePosition -1 30 90
                TeamAddOrRemoveActor 0 1
                BigMapEnableRegion 0 2
                BigMapEnableRegion 1 2
                BigMapEnableRegion 2 2
                BigMapEnableRegion 4 2
                BigMapEnableRegion 5 2
                BigMapEnableRegion 6 1
                BigMapEnableRegion 7 2
                BigMapEnableRegion 8 2
                BigMapEnableRegion 9 1
                BigMapEnableRegion 10 2
                CameraFadeIn"},
            {"失散-双溪", @"
                ScriptVarSetValue -32768 80100
                SceneLoad m07 3
                ActorActivate -1 1
                ActorEnablePlayerControl -1
                PlayerEnableInput 1
                ActorSetNavLayer -1 0
                ActorSetTilePosition -1 14 90
                TeamAddOrRemoveActor 0 1
                TeamAddOrRemoveActor 1 1
                TeamAddOrRemoveActor 2 1
                BigMapEnableRegion 0 2
                BigMapEnableRegion 1 2
                BigMapEnableRegion 2 2
                BigMapEnableRegion 4 2
                BigMapEnableRegion 5 2
                BigMapEnableRegion 6 1
                BigMapEnableRegion 7 2
                BigMapEnableRegion 8 2
                BigMapEnableRegion 9 1
                BigMapEnableRegion 10 2
                CameraFadeIn"},
            {"双溪-里蜀山", @"
                ScriptVarSetValue -32768 80700
                SceneLoad q04 wb
                ActorActivate -1 1
                ActorEnablePlayerControl -1
                PlayerEnableInput 1
                ActorSetNavLayer -1 0
                ActorSetTilePosition -1 311 124
                TeamAddOrRemoveActor 0 1
                TeamAddOrRemoveActor 1 1
                TeamAddOrRemoveActor 3 1
                TeamAddOrRemoveActor 2 1
                BigMapEnableRegion 0 2
                BigMapEnableRegion 1 2
                BigMapEnableRegion 2 2
                BigMapEnableRegion 4 2
                BigMapEnableRegion 5 2
                BigMapEnableRegion 6 1
                BigMapEnableRegion 7 2
                BigMapEnableRegion 8 2
                BigMapEnableRegion 9 1
                BigMapEnableRegion 10 2
                CameraFadeIn"},
            {"里蜀山外城南-太阴归尘", @"
                ScriptVarSetValue -32768 80700
                SceneLoad q04 wn
                ActorActivate -1 1
                ActorEnablePlayerControl -1
                PlayerEnableInput 1
                ActorSetNavLayer -1 0
                ActorSetTilePosition -1 41 428
                TeamAddOrRemoveActor 0 1
                TeamAddOrRemoveActor 1 1
                TeamAddOrRemoveActor 3 1
                TeamAddOrRemoveActor 2 1
                BigMapEnableRegion 0 2
                BigMapEnableRegion 1 2
                BigMapEnableRegion 2 2
                BigMapEnableRegion 4 2
                BigMapEnableRegion 5 2
                BigMapEnableRegion 6 1
                BigMapEnableRegion 7 2
                BigMapEnableRegion 8 2
                BigMapEnableRegion 9 1
                BigMapEnableRegion 10 2
                CameraFadeIn"},
            {"太阴归尘-蜀山故道", @"
                ScriptVarSetValue -32768 90300
                SceneLoad m10 1
                ActorActivate -1 1
                ActorEnablePlayerControl -1
                PlayerEnableInput 1
                ActorSetNavLayer -1 1
                ActorSetTilePosition -1 265 400
                TeamAddOrRemoveActor 0 1
                TeamAddOrRemoveActor 1 1
                BigMapEnableRegion 0 2
                BigMapEnableRegion 1 2
                BigMapEnableRegion 2 2
                BigMapEnableRegion 4 2
                BigMapEnableRegion 5 2
                BigMapEnableRegion 6 1
                BigMapEnableRegion 7 2
                BigMapEnableRegion 8 2
                BigMapEnableRegion 9 1
                BigMapEnableRegion 10 2
                CameraFadeIn"},
            {"蜀山故道-蜀山", @"
                ScriptVarSetValue -32768 91000
                SceneLoad m10 2
                ActorActivate -1 1
                ActorEnablePlayerControl -1
                PlayerEnableInput 1
                ActorSetNavLayer -1 0
                ActorSetTilePosition -1 192 27
                TeamAddOrRemoveActor 0 1
                TeamAddOrRemoveActor 1 1
                BigMapEnableRegion 0 2
                BigMapEnableRegion 1 2
                BigMapEnableRegion 2 2
                BigMapEnableRegion 4 2
                BigMapEnableRegion 5 2
                BigMapEnableRegion 6 1
                BigMapEnableRegion 7 2
                BigMapEnableRegion 8 2
                BigMapEnableRegion 9 1
                BigMapEnableRegion 10 2
                CameraFadeIn"},
            {"蜀山深夜-养父常纪房间", @"
                ScriptVarSetValue -32768 100400
                SceneLoad q02 qTY
                ActorActivate -1 1
                ActorEnablePlayerControl -1
                PlayerEnableInput 1
                ActorSetNavLayer -1 0
                ActorSetTilePosition -1 89 116
                TeamAddOrRemoveActor 0 1
                TeamAddOrRemoveActor 1 1
                TeamAddOrRemoveActor 4 1
                BigMapEnableRegion 0 2
                BigMapEnableRegion 1 2
                BigMapEnableRegion 2 2
                BigMapEnableRegion 4 2
                BigMapEnableRegion 5 2
                BigMapEnableRegion 6 1
                BigMapEnableRegion 7 2
                BigMapEnableRegion 8 2
                BigMapEnableRegion 9 1
                BigMapEnableRegion 10 2
                BigMapEnableRegion 11 1
                CameraFadeIn"},
            {"无极阁找掌门去锁妖塔", @"
                ScriptVarSetValue -32768 100701
                SceneLoad q02 hn01
                ActorActivate -1 1
                ActorEnablePlayerControl -1
                PlayerEnableInput 1
                ActorSetNavLayer -1 0
                ActorSetTilePosition -1 25 17
                TeamAddOrRemoveActor 0 1
                TeamAddOrRemoveActor 1 1
                TeamAddOrRemoveActor 4 1
                BigMapEnableRegion 0 2
                BigMapEnableRegion 1 2
                BigMapEnableRegion 2 2
                BigMapEnableRegion 4 2
                BigMapEnableRegion 5 2
                BigMapEnableRegion 6 1
                BigMapEnableRegion 7 2
                BigMapEnableRegion 8 2
                BigMapEnableRegion 9 1
                BigMapEnableRegion 10 2
                BigMapEnableRegion 11 1
                CameraSetDefaultTransform 2
                CameraFadeIn"},
            {"新安当", @"
                ScriptVarSetValue -32768 101500
                SceneLoad q09 d
                ActorActivate -1 1
                ActorEnablePlayerControl -1
                PlayerEnableInput 1
                ActorSetNavLayer -1 0
                ActorSetTilePosition -1 50 21
                TeamAddOrRemoveActor 0 1
                TeamAddOrRemoveActor 1 1
                TeamAddOrRemoveActor 4 1
                BigMapEnableRegion 0 2
                BigMapEnableRegion 1 2
                BigMapEnableRegion 2 2
                BigMapEnableRegion 4 2
                BigMapEnableRegion 5 2
                BigMapEnableRegion 6 1
                BigMapEnableRegion 7 2
                BigMapEnableRegion 8 2
                BigMapEnableRegion 9 1
                BigMapEnableRegion 10 2
                BigMapEnableRegion 11 1
                BigMapEnableRegion 12 1
                BigMapEnableRegion 13 2
                CameraFadeIn"},
            {"蜀山回无极阁", @"
                ScriptVarSetValue -32768 101600
                SceneLoad Q02 HT
                ActorActivate -1 1
                ActorEnablePlayerControl -1
                PlayerEnableInput 1
                ActorSetNavLayer -1 0
                ActorSetTilePosition -1 365 194
                TeamAddOrRemoveActor 0 1
                TeamAddOrRemoveActor 1 1
                BigMapEnableRegion 0 2
                BigMapEnableRegion 1 2
                BigMapEnableRegion 2 2
                BigMapEnableRegion 4 2
                BigMapEnableRegion 5 2
                BigMapEnableRegion 6 1
                BigMapEnableRegion 7 2
                BigMapEnableRegion 8 2
                BigMapEnableRegion 9 1
                BigMapEnableRegion 10 2
                BigMapEnableRegion 11 1
                BigMapEnableRegion 13 2
                CameraFadeIn"},
            {"地脉门户-太阳华池", @"
                ScriptVarSetValue -32768 110200
                SceneLoad m12 1
                ActorActivate -1 1
                ActorEnablePlayerControl -1
                PlayerEnableInput 1
                ActorSetNavLayer -1 0
                ActorSetTilePosition -1 123 226
                TeamAddOrRemoveActor 0 1
                BigMapEnableRegion 0 2
                BigMapEnableRegion 1 2
                BigMapEnableRegion 2 2
                BigMapEnableRegion 4 2
                BigMapEnableRegion 5 2
                BigMapEnableRegion 6 1
                BigMapEnableRegion 7 2
                BigMapEnableRegion 8 2
                BigMapEnableRegion 9 1
                BigMapEnableRegion 10 2
                BigMapEnableRegion 11 1
                BigMapEnableRegion 13 2
                CameraFadeIn"},
            {"小秘屋会和", @"
                ScriptVarSetValue -32768 120300
                SceneLoad q04 wn
                ActorActivate -1 1
                ActorEnablePlayerControl -1
                PlayerEnableInput 1
                ActorSetNavLayer -1 0
                ActorSetTilePosition -1 112 160
                TeamAddOrRemoveActor 0 1
                TeamAddOrRemoveActor 2 1
                BigMapEnableRegion 0 2
                BigMapEnableRegion 1 2
                BigMapEnableRegion 2 2
                BigMapEnableRegion 4 2
                BigMapEnableRegion 5 2
                BigMapEnableRegion 6 1
                BigMapEnableRegion 7 2
                BigMapEnableRegion 8 2
                BigMapEnableRegion 9 1
                BigMapEnableRegion 10 2
                BigMapEnableRegion 11 1
                BigMapEnableRegion 13 2
                CameraFadeIn"},
            {"里蜀山-魔界之门", @"
                ScriptVarSetValue -32768 120400
                SceneLoad q04 wn
                ActorActivate -1 1
                ActorEnablePlayerControl -1
                PlayerEnableInput 1
                ActorSetNavLayer -1 0
                ActorSetTilePosition -1 195 394
                TeamAddOrRemoveActor 0 1
                TeamAddOrRemoveActor 2 1
                TeamAddOrRemoveActor 3 1
                BigMapEnableRegion 0 2
                BigMapEnableRegion 1 2
                BigMapEnableRegion 2 2
                BigMapEnableRegion 4 2
                BigMapEnableRegion 5 2
                BigMapEnableRegion 6 1
                BigMapEnableRegion 7 2
                BigMapEnableRegion 8 2
                BigMapEnableRegion 9 1
                BigMapEnableRegion 10 2
                BigMapEnableRegion 11 1
                BigMapEnableRegion 13 2
                CameraFadeIn"},
            {"回秘密基地看望星璇", @"
                ScriptVarSetValue -32768 121100
                SceneLoad q04 wn
                ActorActivate -1 1
                ActorEnablePlayerControl -1
                PlayerEnableInput 1
                ActorSetNavLayer -1 0
                ActorSetTilePosition -1 113 161
                TeamAddOrRemoveActor 0 1
                TeamAddOrRemoveActor 2 1
                TeamAddOrRemoveActor 3 1
                BigMapEnableRegion 0 2
                BigMapEnableRegion 1 2
                BigMapEnableRegion 2 2
                BigMapEnableRegion 4 2
                BigMapEnableRegion 5 2
                BigMapEnableRegion 6 1
                BigMapEnableRegion 7 2
                BigMapEnableRegion 8 2
                BigMapEnableRegion 9 1
                BigMapEnableRegion 10 2
                BigMapEnableRegion 11 1
                BigMapEnableRegion 13 2
                CameraFadeIn"},
            {"里蜀山外城北-少阴凝碧", @"
                ScriptVarSetValue -32768 130100
                SceneLoad m14 1
                ActorActivate -1 1
                ActorEnablePlayerControl -1
                PlayerEnableInput 1
                ActorSetNavLayer -1 0
                ActorSetTilePosition -1 122 111
                TeamAddOrRemoveActor 0 1
                TeamAddOrRemoveActor 2 1
                TeamAddOrRemoveActor 3 1
                BigMapEnableRegion 0 2
                BigMapEnableRegion 1 2
                BigMapEnableRegion 2 2
                BigMapEnableRegion 4 2
                BigMapEnableRegion 5 2
                BigMapEnableRegion 6 1
                BigMapEnableRegion 7 2
                BigMapEnableRegion 8 2
                BigMapEnableRegion 9 1
                BigMapEnableRegion 10 2
                BigMapEnableRegion 11 1
                BigMapEnableRegion 13 2
                CameraFadeIn"},
            {"施洞", @"
                ScriptVarSetValue -32768 140200
                SceneLoad q10 q10
                ActorActivate -1 1
                ActorEnablePlayerControl -1
                PlayerEnableInput 1
                ActorSetNavLayer -1 0
                ActorSetTilePosition -1 383 310
                TeamAddOrRemoveActor 0 1
                TeamAddOrRemoveActor 1 1
                TeamAddOrRemoveActor 2 1
                TeamAddOrRemoveActor 4 1
                BigMapEnableRegion 0 2
                BigMapEnableRegion 1 2
                BigMapEnableRegion 2 2
                BigMapEnableRegion 4 2
                BigMapEnableRegion 5 2
                BigMapEnableRegion 6 1
                BigMapEnableRegion 7 2
                BigMapEnableRegion 8 2
                BigMapEnableRegion 9 1
                BigMapEnableRegion 10 2
                BigMapEnableRegion 11 1
                BigMapEnableRegion 13 2
                CameraFadeIn"},
            {"出发去月光城", @"
                ScriptVarSetValue -32768 140400
                SceneLoad q10 q10y
                ActorActivate -1 1
                ActorEnablePlayerControl -1
                PlayerEnableInput 1
                ActorSetNavLayer -1 0
                ActorSetTilePosition -1 301 192
                TeamAddOrRemoveActor 0 1
                TeamAddOrRemoveActor 1 1
                TeamAddOrRemoveActor 2 1
                TeamAddOrRemoveActor 4 1
                BigMapEnableRegion 0 2
                BigMapEnableRegion 1 2
                BigMapEnableRegion 2 2
                BigMapEnableRegion 4 2
                BigMapEnableRegion 5 2
                BigMapEnableRegion 6 1
                BigMapEnableRegion 7 2
                BigMapEnableRegion 8 2
                BigMapEnableRegion 9 1
                BigMapEnableRegion 10 2
                BigMapEnableRegion 11 1
                BigMapEnableRegion 13 2
                CameraFadeIn"},
            {"从施洞回到蜀山", @"
                ScriptVarSetValue -32768 150100
                SceneLoad Q02 HS
                ActorActivate -1 1
                ActorEnablePlayerControl -1
                PlayerEnableInput 1
                ActorSetNavLayer -1 0
                ActorSetTilePosition -1 342 136
                TeamAddOrRemoveActor 0 1
                TeamAddOrRemoveActor 1 1
                TeamAddOrRemoveActor 4 1
                BigMapEnableRegion 0 2
                BigMapEnableRegion 1 2
                BigMapEnableRegion 2 2
                BigMapEnableRegion 4 2
                BigMapEnableRegion 5 2
                BigMapEnableRegion 6 1
                BigMapEnableRegion 7 2
                BigMapEnableRegion 8 2
                BigMapEnableRegion 9 1
                BigMapEnableRegion 10 2
                BigMapEnableRegion 11 1
                BigMapEnableRegion 13 2
                BigMapEnableRegion 14 2
                CameraFadeIn"},
            {"蜀山-绿萝山", @"
                ScriptVarSetValue -32768 150300
                SceneLoad Q02 HS
                ActorActivate -1 1
                ActorEnablePlayerControl -1
                PlayerEnableInput 1
                ActorSetNavLayer -1 0
                ActorSetTilePosition -1 343 140
                TeamAddOrRemoveActor 0 1
                TeamAddOrRemoveActor 1 1
                TeamAddOrRemoveActor 4 1
                BigMapEnableRegion 0 2
                BigMapEnableRegion 1 2
                BigMapEnableRegion 2 2
                BigMapEnableRegion 4 2
                BigMapEnableRegion 5 2
                BigMapEnableRegion 6 1
                BigMapEnableRegion 7 2
                BigMapEnableRegion 8 2
                BigMapEnableRegion 9 1
                BigMapEnableRegion 10 2
                BigMapEnableRegion 11 1
                BigMapEnableRegion 13 2
                BigMapEnableRegion 14 2
                CameraFadeIn"},
            {"地脉门户-少阳参天", @"
                ScriptVarSetValue -32768 150400
                SceneLoad m02 2
                ActorActivate -1 1
                ActorEnablePlayerControl -1
                PlayerEnableInput 1
                ActorSetNavLayer -1 0
                ActorSetTilePosition -1 76 45
                TeamAddOrRemoveActor 0 1
                TeamAddOrRemoveActor 1 1
                BigMapEnableRegion 0 2
                BigMapEnableRegion 1 2
                BigMapEnableRegion 2 2
                BigMapEnableRegion 4 2
                BigMapEnableRegion 5 2
                BigMapEnableRegion 6 1
                BigMapEnableRegion 7 2
                BigMapEnableRegion 8 2
                BigMapEnableRegion 9 1
                BigMapEnableRegion 10 2
                BigMapEnableRegion 11 1
                BigMapEnableRegion 13 2
                BigMapEnableRegion 14 2
                CameraFadeIn"},
            {"回里蜀山秘密基地", @"
                ScriptVarSetValue -32768 160101
                SceneLoad q04 wn
                ActorActivate -1 1
                ActorEnablePlayerControl -1
                PlayerEnableInput 1
                ActorSetNavLayer -1 0
                ActorSetTilePosition -1 115 173
                TeamAddOrRemoveActor 0 1
                TeamAddOrRemoveActor 1 1
                TeamAddOrRemoveActor 2 1
                BigMapEnableRegion 0 2
                BigMapEnableRegion 1 2
                BigMapEnableRegion 2 2
                BigMapEnableRegion 4 2
                BigMapEnableRegion 5 2
                BigMapEnableRegion 6 1
                BigMapEnableRegion 7 2
                BigMapEnableRegion 8 2
                BigMapEnableRegion 9 1
                BigMapEnableRegion 10 2
                BigMapEnableRegion 11 1
                BigMapEnableRegion 13 2
                BigMapEnableRegion 14 2
                CameraFadeIn"},
            {"里蜀山见燎日", @"
                ScriptVarSetValue -32768 161200
                SceneLoad Q04 N
                ActorActivate -1 1
                ActorEnablePlayerControl -1
                PlayerEnableInput 1
                ActorSetNavLayer -1 0
                ActorSetTilePosition -1 191 162
                TeamAddOrRemoveActor 0 1
                TeamAddOrRemoveActor 1 1
                TeamAddOrRemoveActor 2 1
                TeamAddOrRemoveActor 3 1
                BigMapEnableRegion 0 2
                BigMapEnableRegion 1 2
                BigMapEnableRegion 2 2
                BigMapEnableRegion 4 2
                BigMapEnableRegion 5 2
                BigMapEnableRegion 6 1
                BigMapEnableRegion 7 2
                BigMapEnableRegion 8 2
                BigMapEnableRegion 9 1
                BigMapEnableRegion 10 2
                BigMapEnableRegion 11 1
                BigMapEnableRegion 13 2
                BigMapEnableRegion 14 2
                CameraFadeIn"},
            {"里蜀山内城到厥阴蔽日", @"
                ScriptVarSetValue -32768 170200
                SceneLoad Q04 N
                ActorActivate -1 1
                ActorEnablePlayerControl -1
                PlayerEnableInput 1
                ActorSetNavLayer -1 0
                ActorSetTilePosition -1 403 116
                TeamAddOrRemoveActor 0 1
                TeamAddOrRemoveActor 1 1
                TeamAddOrRemoveActor 2 1
                TeamAddOrRemoveActor 3 1
                BigMapEnableRegion 0 2
                BigMapEnableRegion 1 2
                BigMapEnableRegion 2 2
                BigMapEnableRegion 4 2
                BigMapEnableRegion 5 2
                BigMapEnableRegion 6 1
                BigMapEnableRegion 7 2
                BigMapEnableRegion 8 2
                BigMapEnableRegion 9 1
                BigMapEnableRegion 10 2
                BigMapEnableRegion 11 1
                BigMapEnableRegion 13 2
                BigMapEnableRegion 14 2
                CameraFadeIn"},
            {"地脉门户-蜀山前山", @"
                ScriptVarSetValue -32768 171200
                SceneLoad m02 1
                ActorActivate -1 1
                ActorEnablePlayerControl -1
                PlayerEnableInput 1
                ActorSetNavLayer -1 0
                ActorSetTilePosition -1 132 155
                TeamAddOrRemoveActor 0 1
                TeamAddOrRemoveActor 1 1
                BigMapEnableRegion 0 2
                BigMapEnableRegion 1 2
                BigMapEnableRegion 2 2
                BigMapEnableRegion 4 2
                BigMapEnableRegion 5 2
                BigMapEnableRegion 6 1
                BigMapEnableRegion 7 2
                BigMapEnableRegion 8 2
                BigMapEnableRegion 9 1
                BigMapEnableRegion 10 2
                BigMapEnableRegion 11 1
                BigMapEnableRegion 13 2
                BigMapEnableRegion 14 2
                CameraFadeIn"},
            {"花圃找王蓬絮\n(温慧好感度最高)", @"
                ScriptVarSetValue -32768 180600
                SceneLoad q02 q
                ActorActivate -1 1
                ActorEnablePlayerControl -1
                PlayerEnableInput 1
                ActorSetNavLayer -1 0
                ActorSetTilePosition -1 353 98
                TeamAddOrRemoveActor 0 1
                TeamAddOrRemoveActor 1 1
                BigMapEnableRegion 0 2
                BigMapEnableRegion 1 2
                BigMapEnableRegion 2 2
                BigMapEnableRegion 4 2
                BigMapEnableRegion 5 2
                BigMapEnableRegion 6 1
                BigMapEnableRegion 7 2
                BigMapEnableRegion 8 2
                BigMapEnableRegion 9 1
                BigMapEnableRegion 10 2
                BigMapEnableRegion 11 1
                BigMapEnableRegion 13 2
                BigMapEnableRegion 14 2
                FavorAdd 1 50
                FavorAdd 2 10
                CameraFadeIn"},
            {"花圃找王蓬絮\n(王蓬絮好感度最高)", @"
                ScriptVarSetValue -32768 180600
                SceneLoad q02 q
                ActorActivate -1 1
                ActorEnablePlayerControl -1
                PlayerEnableInput 1
                ActorSetNavLayer -1 0
                ActorSetTilePosition -1 353 98
                TeamAddOrRemoveActor 0 1
                TeamAddOrRemoveActor 1 1
                BigMapEnableRegion 0 2
                BigMapEnableRegion 1 2
                BigMapEnableRegion 2 2
                BigMapEnableRegion 4 2
                BigMapEnableRegion 5 2
                BigMapEnableRegion 6 1
                BigMapEnableRegion 7 2
                BigMapEnableRegion 8 2
                BigMapEnableRegion 9 1
                BigMapEnableRegion 10 2
                BigMapEnableRegion 11 1
                BigMapEnableRegion 13 2
                BigMapEnableRegion 14 2
                FavorAdd 1 10
                FavorAdd 2 50
                CameraFadeIn"},
            #endif
        };

        public void Init(InputManager inputManager,
            EventSystem eventSystem,
            SceneManager sceneManager,
            GameStateManager gameStateManager,
            ScriptManager scriptManager,
            TeamManager teamManager,
            SaveManager saveManager,
            InformationManager informationManager,
            CanvasGroup storySelectorCanvas,
            GameObject storySelectorButtonPrefab)
        {
            _inputManager = inputManager ?? throw new ArgumentNullException(nameof(inputManager));
            _eventSystem = eventSystem != null ? eventSystem : throw new ArgumentNullException(nameof(eventSystem));
            _sceneManager = sceneManager ?? throw new ArgumentNullException(nameof(sceneManager));
            _gameStateManager = gameStateManager ?? throw new ArgumentNullException(nameof(gameStateManager));
            _playerInputActions = inputManager.GetPlayerInputActions();
            _scriptManager = scriptManager ?? throw new ArgumentNullException(nameof(scriptManager));
            _teamManager = teamManager ?? throw new ArgumentNullException(nameof(teamManager));
            _saveManager = saveManager ?? throw new ArgumentNullException(nameof(saveManager));
            _informationManager = informationManager != null ? informationManager : throw new ArgumentNullException(nameof(informationManager));
            _storySelectorCanvas = storySelectorCanvas != null ? storySelectorCanvas : throw new ArgumentNullException(nameof(storySelectorCanvas));
            _storySelectorButtonPrefab = storySelectorButtonPrefab != null ? storySelectorButtonPrefab : throw new ArgumentNullException(nameof(storySelectorButtonPrefab));

            _storySelectorCanvas.alpha = 0f;
            _storySelectorCanvas.interactable = false;

            _playerInputActions.Gameplay.ToggleStorySelector.performed += ToggleStorySelectorOnPerformed;
            _playerInputActions.Cutscene.ToggleStorySelector.performed += ToggleStorySelectorOnPerformed;
            _playerInputActions.Cutscene.ExitCurrentShowingMenu.performed += HideStorySelectorOnPerformed;
        }

        private void OnEnable()
        {
            CommandExecutorRegistry<ICommand>.Instance.Register(this);
        }

        private void OnDisable()
        {
            CommandExecutorRegistry<ICommand>.Instance.UnRegister(this);
            _playerInputActions.Gameplay.ToggleStorySelector.performed -= ToggleStorySelectorOnPerformed;
            _playerInputActions.Cutscene.ToggleStorySelector.performed -= ToggleStorySelectorOnPerformed;
            _playerInputActions.Cutscene.ExitCurrentShowingMenu.performed -= HideStorySelectorOnPerformed;
        }

        private void ToggleStorySelectorOnPerformed(InputAction.CallbackContext _)
        {
            ToggleStorySelector();
        }

        private void HideStorySelectorOnPerformed(InputAction.CallbackContext _)
        {
            if (_sceneManager.GetCurrentScene() == null) return;

            if (_storySelectorCanvas.interactable)
            {
                Hide();
                _gameStateManager.GoToState(GameState.Gameplay);
            }
        }

        private void ToggleStorySelector()
        {
            if (_sceneManager.GetCurrentScene() == null) return;

            if (_storySelectorCanvas.interactable)
            {
                Hide();
                _gameStateManager.GoToState(GameState.Gameplay);
            }
            else if (_gameStateManager.GetCurrentState() == GameState.Gameplay)
            {
                Show();
            }
        }

        public void Show()
        {
            _gameStateManager.GoToState(GameState.Cutscene);
            CommandDispatcher<ICommand>.Instance.Dispatch(
                new ActorStopActionAndStandCommand(ActorConstants.PlayerActorVirtualID));

            var hideCloseButton = _sceneManager.GetCurrentScene() == null;

            foreach (var story in _storySelections)
            {
                if (hideCloseButton && story.Key is "关闭" or "保存当前游戏进度") continue;

                #if !UNITY_STANDALONE || UNITY_EDITOR
                if (story.Key == "退出游戏") continue;
                #endif

                GameObject selectionButton = Instantiate(_storySelectorButtonPrefab, _storySelectorCanvas.transform);

                var buttonTextUI = selectionButton.GetComponentInChildren<TextMeshProUGUI>();
                buttonTextUI.text = story.Key;

                if (story.Key is "关闭" or "保存当前游戏进度" or "退出游戏" or "新的游戏" or "继续游戏")
                {
                    buttonTextUI.fontStyle = FontStyles.Underline;
                }

                var button = selectionButton.GetComponent<Button>();
                Navigation buttonNavigation = button.navigation;
                buttonNavigation.mode = Navigation.Mode.Horizontal | Navigation.Mode.Vertical;
                button.navigation = buttonNavigation;
                button.colors = UITheme.GetButtonColors();
                button.onClick.AddListener(delegate { StorySelectionButtonClicked(story.Key);});
                _selectionButtons.Add(selectionButton);
            }

            var firstButton = _selectionButtons.First().GetComponent<Button>();

            InputDevice lastActiveInputDevice = _inputManager.GetLastActiveInputDevice();
            if (lastActiveInputDevice == Keyboard.current ||
                lastActiveInputDevice == Gamepad.current ||
                lastActiveInputDevice == DualShockGamepad.current)
            {
                _eventSystem.firstSelectedGameObject = firstButton.gameObject;
                firstButton.Select();
            }
            else
            {
                _eventSystem.firstSelectedGameObject = null;
            }

            _storySelectorCanvas.alpha = 1f;
            _storySelectorCanvas.interactable = true;
        }

        private void StorySelectionButtonClicked(string story)
        {
            switch (story)
            {
                case "关闭":
                    if (_sceneManager.GetCurrentScene() == null) return;
                    _gameStateManager.GoToState(GameState.Gameplay);
                    break;
                case "退出游戏":
                    Application.Quit();
                    break;
                case "新的游戏":
                    CommandDispatcher<ICommand>.Instance.Dispatch(new ResetGameStateCommand());
                    StartNewGame();
                    break;
                case "继续游戏":
                    if (!_saveManager.SaveFileExists())
                    {
                        CommandDispatcher<ICommand>.Instance.Dispatch(new ResetGameStateCommand());
                        StartNewGame();
                    }
                    else
                    {
                        var commands = _saveManager.LoadFromSaveFile();
                        if (commands == null)
                        {
                            CommandDispatcher<ICommand>.Instance.Dispatch(new ResetGameStateCommand());
                            StartNewGame();
                        }
                        else
                        {
                            ExecuteCommandsFromSaveFile(commands);
                        }
                    }
                    break;
                case "保存当前游戏进度":
                    CommandDispatcher<ICommand>.Instance.Dispatch(_saveManager.SaveGameStateToFile()
                        ? new UIDisplayNoteCommand("游戏保存成功")
                        : new UIDisplayNoteCommand("游戏保存失败"));
                    _gameStateManager.GoToState(GameState.Gameplay);
                    break;
                default:
                    CommandDispatcher<ICommand>.Instance.Dispatch(new ResetGameStateCommand());
                    ExecuteCommands(_storySelections[story]);
                    break;
            }

            Hide();
        }

        private void StartNewGame()
        {
            // Init main story progress
            _scriptManager.SetGlobalVariable(ScriptConstants.MainStoryVariableName, 0);

            // Add init script
            _scriptManager.AddScript(ScriptConstants.InitScriptId);

            // Add main actor to the team
            _teamManager.AddActor(0);

            _gameStateManager.GoToState(GameState.Cutscene);
        }

        private void ExecuteCommands(string commands)
        {
            foreach (var command in commands.Split('\n'))
            {
                if (!string.IsNullOrEmpty(command))
                {
                    DebugLogConsole.ExecuteCommand(command);
                }
            }
        }

        private void ExecuteCommandsFromSaveFile(string commands)
        {
            _informationManager.EnableNoteDisplay(false);

            _deferredExecutionCommands.Clear();

            var commandsToExecute = new List<string>();

            foreach (var command in commands.Split('\n'))
            {
                if (string.IsNullOrEmpty(command)) continue;

                var arguments = new List<string>();

                DebugLogConsole.FetchArgumentsFromCommand(command, arguments);

                // These commands should be executed after the scene script is executed
                // to prevent unexpected behavior
                if (arguments[0] + "Command" == nameof(ActorActivateCommand) ||
                    arguments[0] + "Command" == nameof(ActorSetNavLayerCommand) ||
                    arguments[0] + "Command" == nameof(ActorSetWorldPositionCommand) ||
                    arguments[0] + "Command" == nameof(ActorSetYPositionCommand) ||
                    arguments[0] + "Command" == nameof(ActorSetFacingCommand) ||
                    arguments[0] + "Command" == nameof(ActorSetScriptCommand))
                {
                    _deferredExecutionCommands.Add(command);
                }
                else
                {
                    commandsToExecute.Add(command);
                }
            }

            foreach (var command in commandsToExecute)
            {
                DebugLogConsole.ExecuteCommand(command);
            }

            _informationManager.EnableNoteDisplay(true);
        }

        public void Hide()
        {
            _storySelectorCanvas.alpha = 0f;
            _storySelectorCanvas.interactable = false;

            foreach (GameObject button in _selectionButtons)
            {
                button.GetComponent<Button>().onClick.RemoveAllListeners();
                Destroy(button);
            }
            _selectionButtons.Clear();
        }

        public void Execute(ToggleStorySelectorRequest command)
        {
            ToggleStorySelector();
        }

        public void Execute(GameSwitchToMainMenuCommand command)
        {
            if (!_storySelectorCanvas.interactable) Show();
        }

        public void Execute(ScenePostLoadingNotification command)
        {
            if (_deferredExecutionCommands.Count == 0) return;

            if (command.SceneScriptId == ScriptConstants.InvalidScriptId)
            {
                ExecuteCommands(string.Join('\n', _deferredExecutionCommands));
                _deferredExecutionCommands.Clear();
            }
            else
            {
                StartCoroutine(ExecuteDeferredCommandsAfterSceneScriptFinished(command.SceneScriptId));
            }
        }

        private IEnumerator ExecuteDeferredCommandsAfterSceneScriptFinished(uint scriptId)
        {
            yield return new WaitUntilScriptFinished(PalScriptType.Scene, scriptId);
            if (_deferredExecutionCommands.Count > 0)
            {
                ExecuteCommands(string.Join('\n', _deferredExecutionCommands));
                _deferredExecutionCommands.Clear();
            }
        }
    }
}