// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Dev
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Command;
    using Command.InternalCommands;
    using IngameDebugConsole;
    using Input;
    using MetaData;
    using Scene;
    using Script;
    using State;
    using TMPro;
    using UnityEngine;
    using UnityEngine.EventSystems;
    using UnityEngine.InputSystem;
    using UnityEngine.UI;

    public class StorySelector : MonoBehaviour,
        ICommandExecutor<ToggleStorySelectorRequest>
    {
        private InputManager _inputManager;
        private EventSystem _eventSystem;
        private PlayerInputActions _playerInputActions;
        private ScriptManager _scriptManager;
        private GameStateManager _gameStateManager;
        private SceneManager _sceneManager;
        private CanvasGroup _storySelectorCanvas;
        private GameObject _storySelectorButtonPrefab;

        private readonly List<GameObject> _selectionButtons = new();

        private readonly Dictionary<string, string> _storySelections = new()
        {
            {"退出", ""},
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
            {"大渡口-初遇长卿紫萱", @"
                ScriptVarSetValue -32768 20500
                SceneLoad m03 1
                ActorActivate -1 1
                ActorEnablePlayerControl -1
                PlayerEnableInput 1
                ActorSetNavLayer -1 0
                ActorSetTilePosition -1 359 370
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
            {"蜀山-从锁妖塔返回", @"
                ScriptVarSetValue -32768 90600
                SceneLoad q08 q08p
                ActorActivate -1 1
                ActorEnablePlayerControl -1
                PlayerEnableInput 1
                ActorSetNavLayer -1 1
                ActorSetTilePosition -1 82 157
                TeamAddOrRemoveActor 0 1
                TeamAddOrRemoveActor 3 1
                TeamAddOrRemoveActor 1 1
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
            #endif
        };

        private double _showTime;

        public void Init(InputManager inputManager,
            EventSystem eventSystem,
            SceneManager sceneManager,
            GameStateManager gameStateManager,
            ScriptManager scriptManager,
            CanvasGroup storySelectorCanvas,
            GameObject storySelectorButtonPrefab)
        {
            _inputManager = inputManager;
            _eventSystem = eventSystem;
            _sceneManager = sceneManager;
            _gameStateManager = gameStateManager;
            _playerInputActions = inputManager.GetPlayerInputActions();
            _scriptManager = scriptManager;
            _storySelectorCanvas = storySelectorCanvas;
            _storySelectorButtonPrefab = storySelectorButtonPrefab;

            _storySelectorCanvas.alpha = 0f;
            _storySelectorCanvas.interactable = false;

            _playerInputActions.Gameplay.ToggleStorySelector.performed += ToggleStorySelectorOnPerformed;
            _playerInputActions.Cutscene.ToggleStorySelector.performed += ToggleStorySelectorOnPerformed;
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
        }

        private void ToggleStorySelectorOnPerformed(InputAction.CallbackContext obj)
        {
            ToggleStorySelector();
        }

        private void ToggleStorySelector()
        {
            if (_storySelectorCanvas.interactable) Hide();
            else if (_gameStateManager.GetCurrentState() == GameState.Gameplay)
            {
                Show();
            }
        }

        public void Show()
        {
            _showTime = Time.realtimeSinceStartupAsDouble;
            
            _gameStateManager.GoToState(GameState.Cutscene);
            foreach (var story in _storySelections)
            {
                var selectionButton = Instantiate(_storySelectorButtonPrefab, _storySelectorCanvas.transform);
                var buttonTextUI = selectionButton.GetComponentInChildren<TextMeshProUGUI>();
                buttonTextUI.text = story.Key;
                selectionButton.GetComponent<Button>().onClick
                    .AddListener(delegate { StorySelectionButtonClicked(story.Key);});
                _selectionButtons.Add(selectionButton);
            }

            // Setup button navigation
            for (var i = 0; i < _selectionButtons.Count; i++)
            {
                var button = _selectionButtons[i].GetComponent<Button>();
                var buttonNavigation = button.navigation;
                buttonNavigation.mode = Navigation.Mode.Explicit;

                if (i == 0)
                {
                    buttonNavigation.selectOnRight = _selectionButtons[i + 1].GetComponent<Button>();
                }
                else if (i == _selectionButtons.Count - 1)
                {
                    buttonNavigation.selectOnLeft = _selectionButtons[i - 1].GetComponent<Button>();
                }
                else
                {
                    buttonNavigation.selectOnLeft = _selectionButtons[i - 1].GetComponent<Button>();
                    buttonNavigation.selectOnRight = _selectionButtons[i + 1].GetComponent<Button>();
                }

                button.navigation = buttonNavigation;
            }

            var firstButton = _selectionButtons.First().GetComponent<Button>();
            _eventSystem.firstSelectedGameObject = firstButton.gameObject;

            var lastActiveInputDevice = _inputManager.GetLastActiveInputDevice();
            if (lastActiveInputDevice == Keyboard.current ||
                lastActiveInputDevice == Gamepad.current)
            {
                firstButton.Select();
            }

            _storySelectorCanvas.alpha = 1f;
            _storySelectorCanvas.interactable = true;
        }

        private void StorySelectionButtonClicked(string story)
        {
            if (Time.realtimeSinceStartupAsDouble - _showTime < 1f) return;

            switch (story)
            {
                case "退出":
                    if (_sceneManager.GetCurrentScene() == null) return;
                    break;
                case "新的游戏":
                    CommandDispatcher<ICommand>.Instance.Dispatch(new ResetGameStateCommand());
                    StartNewGame();
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

        public void Hide()
        {
            _storySelectorCanvas.alpha = 0f;
            _storySelectorCanvas.interactable = false;

            foreach (var button in _selectionButtons)
            {
                button.GetComponent<Button>().onClick.RemoveAllListeners();
                Destroy(button);
            }
            _selectionButtons.Clear();
            _gameStateManager.GoToState(GameState.Gameplay);
        }

        public void Execute(ToggleStorySelectorRequest command)
        {
            ToggleStorySelector();
        }
    }
}