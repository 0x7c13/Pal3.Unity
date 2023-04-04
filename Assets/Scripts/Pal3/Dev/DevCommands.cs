// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Dev
{
    using System.Collections.Generic;

    public static class DevCommands
    {
        #region Story Selections
        public static readonly Dictionary<string, string> StoryJumpPoints = new()
        {
            #if PAL3
            {"渝州\n\n去唐家堡找雪见", @"
                ScriptVarSetValue -32768 10401
                SceneLoad q01 b
                ActorActivate -1 1
                ActorEnablePlayerControl -1
                PlayerEnableInput 1
                ActorSetNavLayer -1 0
                ActorSetTilePosition -1 195 212
                TeamAddOrRemoveActor 0 1
                CameraFadeIn"},
            {"渝州\n\n第一次见重楼前", @"
                ScriptVarSetValue -32768 10800
                SceneLoad q01 ba
                ActorActivate -1 1
                ActorEnablePlayerControl -1
                PlayerEnableInput 1
                ActorSetNavLayer -1 0
                ActorSetTilePosition -1 190 230
                TeamAddOrRemoveActor 0 1
                CameraFadeIn"},
            {"渝州西南\n\n入住客栈过夜", @"
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
            {"宾化\n\n寻找雪见", @"
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
            {"镇江\n\n第一次进入", @"
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
            {"蓬莱御剑堂\n\n初遇邪剑仙", @"
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
            {"渝州\n\n第一次见龙葵前", @"
                ScriptVarSetValue -32768 30800
                SceneLoad q01 ba
                ActorActivate -1 1
                ActorEnablePlayerControl -1
                PlayerEnableInput 1
                ActorSetNavLayer -1 0
                ActorSetTilePosition -1 277 293
                TeamAddOrRemoveActor 0 1
                CameraFadeIn"},
            {"渝州西南\n\n找到雪见", @"
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
            {"德阳\n\n第一次进入", @"
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
            {"安宁村\n\n第一次进入", @"
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
            {"安宁村\n\n返回万玉枝家", @"
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
            {"蜀山故道\n\n初入蜀山前", @"
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
            {"唐家堡\n\n从蜀山返回", @"
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
            {"雷州\n\n第一次进入", @"
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
            {"刺史府\n\n去神魔之井前", @"
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
            {"神界天门\n\n第一次进入", @"
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
            {"锁妖塔\n\n第一次进入", @"
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
            {"蛮州\n\n第一次进入", @"
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
            {"古城镇\n\n第一次进入", @"
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
            {"上祭坛之前\n\n雪见最高好感", @"
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
            {"上祭坛之前\n\n龙葵最高好感", @"
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
            {"酆都\n\n第一次进入", @"
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
            {"鬼界外围\n\n第一次进入", @"
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
            {"蜀山\n\n从黄泉路返回后", @"
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
            {"雪岭镇\n\n第一次进入", @"
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
            {"安溪\n\n第一次进入", @"
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
            {"双剑选择\n\n雪见最高好感", @"
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
            {"双剑选择\n\n龙葵最高好感", @"
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
            {"双剑选择\n\n紫萱最高好感", @"
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
            {"双剑选择\n\n花楹最高好感", @"
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
            {"唐家堡\n\n登云麓", @"
                ScriptVarSetValue -32768 10800
                SceneLoad Q01 Q01
                ActorActivate -1 1
                ActorEnablePlayerControl -1
                PlayerEnableInput 1
                ActorSetNavLayer -1 0
                ActorSetTilePosition -1 190 20
                TeamAddOrRemoveActor 0 1
                CameraFadeIn"},
            {"蜀山\n\n第一次回家", @"
                ScriptVarSetValue -32768 20800
                SceneLoad q02 qH
                ActorActivate -1 1
                ActorEnablePlayerControl -1
                PlayerEnableInput 1
                ActorSetNavLayer -1 0
                ActorSetTilePosition -1 91 114
                TeamAddOrRemoveActor 0 1
                CameraFadeIn"},
            {"蜀山\n\n绿萝嶂", @"
                ScriptVarSetValue -32768 30101
                SceneSaveGlobalObjectSwitchState m02 1 35 1
                SceneSaveGlobalObjectTimesCount m02 1 35 0
                SceneSaveGlobalObjectActivationState m02 1 36 False
                SceneSaveGlobalObjectSwitchState m02 1 36 1
                SceneLoad Q02 HH
                ActorActivate -1 1
                ActorEnablePlayerControl -1
                PlayerEnableInput 1
                ActorSetNavLayer -1 0
                ActorSetTilePosition -1 342 137
                TeamAddOrRemoveActor 0 1
                TeamAddOrRemoveActor 1 1
                CameraFadeIn"},
            {"地脉门户\n\n少阳三焦", @"
                ScriptVarSetValue -32768 40200
                SceneSaveGlobalObjectSwitchState m02 1 35 1
                SceneSaveGlobalObjectTimesCount m02 1 35 0
                SceneSaveGlobalObjectActivationState m02 1 36 False
                SceneSaveGlobalObjectSwitchState m02 1 36 1
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
                SceneSaveGlobalObjectSwitchState m02 1 35 1
                SceneSaveGlobalObjectTimesCount m02 1 35 0
                SceneSaveGlobalObjectActivationState m02 1 36 False
                SceneSaveGlobalObjectSwitchState m02 1 36 1
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
            {"里蜀山外城南\n\n厥阴心包", @"
                ScriptVarSetValue -32768 50200
                SceneSaveGlobalObjectSwitchState m02 1 35 1
                SceneSaveGlobalObjectTimesCount m02 1 35 0
                SceneSaveGlobalObjectActivationState m02 1 36 False
                SceneSaveGlobalObjectSwitchState m02 1 36 1
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
            {"厥阴心包\n\n胜州", @"
                ScriptVarSetValue -32768 50500
                SceneSaveGlobalObjectSwitchState m02 1 35 1
                SceneSaveGlobalObjectTimesCount m02 1 35 0
                SceneSaveGlobalObjectActivationState m02 1 36 False
                SceneSaveGlobalObjectSwitchState m02 1 36 1
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
            {"蜀山\n\n深夜去经库", @"
                ScriptVarSetValue -32768 60500
                SceneSaveGlobalObjectSwitchState m02 1 35 1
                SceneSaveGlobalObjectTimesCount m02 1 35 0
                SceneSaveGlobalObjectActivationState m02 1 36 False
                SceneSaveGlobalObjectSwitchState m02 1 36 1
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
            {"地脉门户大厅\n\n阳名百纳", @"
                ScriptVarSetValue -32768 70100
                SceneSaveGlobalObjectSwitchState m02 1 35 1
                SceneSaveGlobalObjectTimesCount m02 1 35 0
                SceneSaveGlobalObjectActivationState m02 1 36 False
                SceneSaveGlobalObjectSwitchState m02 1 36 1
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
            {"血濡回魂\n\n初入京城", @"
                ScriptVarSetValue -32768 70700
                SceneSaveGlobalObjectSwitchState m02 1 35 1
                SceneSaveGlobalObjectTimesCount m02 1 35 0
                SceneSaveGlobalObjectActivationState m02 1 36 False
                SceneSaveGlobalObjectSwitchState m02 1 36 1
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
            {"失散\n\n双溪", @"
                ScriptVarSetValue -32768 80100
                SceneSaveGlobalObjectSwitchState m02 1 35 1
                SceneSaveGlobalObjectTimesCount m02 1 35 0
                SceneSaveGlobalObjectActivationState m02 1 36 False
                SceneSaveGlobalObjectSwitchState m02 1 36 1
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
            {"双溪\n\n里蜀山", @"
                ScriptVarSetValue -32768 80700
                SceneSaveGlobalObjectSwitchState m02 1 35 1
                SceneSaveGlobalObjectTimesCount m02 1 35 0
                SceneSaveGlobalObjectActivationState m02 1 36 False
                SceneSaveGlobalObjectSwitchState m02 1 36 1
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
            {"里蜀山外城南\n\n太阴归尘", @"
                ScriptVarSetValue -32768 80700
                SceneSaveGlobalObjectSwitchState m02 1 35 1
                SceneSaveGlobalObjectTimesCount m02 1 35 0
                SceneSaveGlobalObjectActivationState m02 1 36 False
                SceneSaveGlobalObjectSwitchState m02 1 36 1
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
            {"太阴归尘\n\n蜀山故道", @"
                ScriptVarSetValue -32768 90300
                SceneSaveGlobalObjectSwitchState m02 1 35 1
                SceneSaveGlobalObjectTimesCount m02 1 35 0
                SceneSaveGlobalObjectActivationState m02 1 36 False
                SceneSaveGlobalObjectSwitchState m02 1 36 1
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
            {"蜀山深夜\n\n养父常纪房间", @"
                ScriptVarSetValue -32768 100400
                SceneSaveGlobalObjectSwitchState m02 1 35 1
                SceneSaveGlobalObjectTimesCount m02 1 35 0
                SceneSaveGlobalObjectActivationState m02 1 36 False
                SceneSaveGlobalObjectSwitchState m02 1 36 1
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
                SceneSaveGlobalObjectSwitchState m02 1 35 1
                SceneSaveGlobalObjectTimesCount m02 1 35 0
                SceneSaveGlobalObjectActivationState m02 1 36 False
                SceneSaveGlobalObjectSwitchState m02 1 36 1
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
                SceneSaveGlobalObjectSwitchState m02 1 35 1
                SceneSaveGlobalObjectTimesCount m02 1 35 0
                SceneSaveGlobalObjectActivationState m02 1 36 False
                SceneSaveGlobalObjectSwitchState m02 1 36 1
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
            {"地脉门户\n\n太阳华池", @"
                ScriptVarSetValue -32768 110200
                SceneSaveGlobalObjectSwitchState m02 1 35 1
                SceneSaveGlobalObjectTimesCount m02 1 35 0
                SceneSaveGlobalObjectActivationState m02 1 36 False
                SceneSaveGlobalObjectSwitchState m02 1 36 1
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
                SceneSaveGlobalObjectSwitchState m02 1 35 1
                SceneSaveGlobalObjectTimesCount m02 1 35 0
                SceneSaveGlobalObjectActivationState m02 1 36 False
                SceneSaveGlobalObjectSwitchState m02 1 36 1
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
            {"里蜀山\n\n魔界之门", @"
                ScriptVarSetValue -32768 120400
                SceneSaveGlobalObjectSwitchState m02 1 35 1
                SceneSaveGlobalObjectTimesCount m02 1 35 0
                SceneSaveGlobalObjectActivationState m02 1 36 False
                SceneSaveGlobalObjectSwitchState m02 1 36 1
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
            {"里蜀山外城北\n\n少阴凝碧", @"
                ScriptVarSetValue -32768 130100
                SceneSaveGlobalObjectSwitchState m02 1 35 1
                SceneSaveGlobalObjectTimesCount m02 1 35 0
                SceneSaveGlobalObjectActivationState m02 1 36 False
                SceneSaveGlobalObjectSwitchState m02 1 36 1
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
                SceneSaveGlobalObjectSwitchState m02 1 35 1
                SceneSaveGlobalObjectTimesCount m02 1 35 0
                SceneSaveGlobalObjectActivationState m02 1 36 False
                SceneSaveGlobalObjectSwitchState m02 1 36 1
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
                SceneSaveGlobalObjectSwitchState m02 1 35 1
                SceneSaveGlobalObjectTimesCount m02 1 35 0
                SceneSaveGlobalObjectActivationState m02 1 36 False
                SceneSaveGlobalObjectSwitchState m02 1 36 1
                SceneLoad q10 q10y
                ActorActivate -1 1
                ActorEnablePlayerControl -1
                PlayerEnableInput 1
                ActorSetNavLayer -1 0
                ActorSetTilePosition -1 144 53
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
            {"蜀山\n\n重回绿萝山", @"
                ScriptVarSetValue -32768 150300
                SceneSaveGlobalObjectSwitchState m02 1 35 1
                SceneSaveGlobalObjectTimesCount m02 1 35 0
                SceneSaveGlobalObjectActivationState m02 1 36 False
                SceneSaveGlobalObjectSwitchState m02 1 36 1
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
            {"地脉门户\n\n少阳参天", @"
                ScriptVarSetValue -32768 150400
                SceneSaveGlobalObjectSwitchState m02 1 35 1
                SceneSaveGlobalObjectTimesCount m02 1 35 0
                SceneSaveGlobalObjectActivationState m02 1 36 False
                SceneSaveGlobalObjectSwitchState m02 1 36 1
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
                SceneSaveGlobalObjectSwitchState m02 1 35 1
                SceneSaveGlobalObjectTimesCount m02 1 35 0
                SceneSaveGlobalObjectActivationState m02 1 36 False
                SceneSaveGlobalObjectSwitchState m02 1 36 1
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
            {"里蜀山内城\n\n厥阴蔽日", @"
                ScriptVarSetValue -32768 170200
                SceneSaveGlobalObjectSwitchState m02 1 35 1
                SceneSaveGlobalObjectTimesCount m02 1 35 0
                SceneSaveGlobalObjectActivationState m02 1 36 False
                SceneSaveGlobalObjectSwitchState m02 1 36 1
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
            {"地脉门户\n\n蜀山前山", @"
                ScriptVarSetValue -32768 171200
                SceneSaveGlobalObjectSwitchState m02 1 35 1
                SceneSaveGlobalObjectTimesCount m02 1 35 0
                SceneSaveGlobalObjectActivationState m02 1 36 False
                SceneSaveGlobalObjectSwitchState m02 1 36 1
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
            {"花圃\n\n温慧最高好感", @"
                ScriptVarSetValue -32768 180600
                SceneSaveGlobalObjectSwitchState m02 1 35 1
                SceneSaveGlobalObjectTimesCount m02 1 35 0
                SceneSaveGlobalObjectActivationState m02 1 36 False
                SceneSaveGlobalObjectSwitchState m02 1 36 1
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
            {"花圃\n\n王蓬絮最高好感", @"
                ScriptVarSetValue -32768 180600
                SceneSaveGlobalObjectSwitchState m02 1 35 1
                SceneSaveGlobalObjectTimesCount m02 1 35 0
                SceneSaveGlobalObjectActivationState m02 1 36 False
                SceneSaveGlobalObjectSwitchState m02 1 36 1
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
        #endregion
    }
}