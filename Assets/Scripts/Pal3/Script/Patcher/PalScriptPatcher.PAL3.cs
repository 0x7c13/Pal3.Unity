// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

#if PAL3

namespace Pal3.Script.Patcher
{
    using System.Collections.Generic;
    using Command;
    using Command.SceCommands;
    using MetaData;

    public sealed class PalScriptPatcher : IPalScriptPatcher
    {
        public Dictionary<string, ICommand> PatchedCommands => new ()
        {
            {
                $"{936}_{PalScriptType.Scene}_{1284}_韩用－小四_{56}_{typeof(DialogueRenderTextCommand)}",
                new DialogueRenderTextCommand($"知道吗？您现在玩的{GameConstants.AppNameCNFull}是由\\i柒才\\r使用C#/Unity开发(完全重写)的复刻版，" +
                                              "免费开源且支持全平台(Win/Mac/Linux/iOS/Android)。" +
                                              "如果您是花钱得到的，那么恭喜您成为盗版游戏的受害者。" +
                                              "当前游戏还在开发中，包括战斗在内的很多功能尚未实现，请耐心等待。" +
                                              "也欢迎加入Q群与作者联系或下载最新版：\\i252315306\\r，" +
                                              "您也可以在B站关注Up主\\i@柒才\\r获取最新开发信息！另外，全平台都支持手柄哦~")
            },
            {
                $"{950}_{PalScriptType.Scene}_{1284}_韓用－小四_{56}_{typeof(DialogueRenderTextCommand)}",
                new DialogueRenderTextCommand($"知道嗎？您現在玩的{GameConstants.AppNameCNFull}是由\\i柒才\\r使用C#/Unity開發（完全重寫）的復刻版，" +
                                              "免費開源且支持全平台(Win/Mac/Linux/iOS/Android)。" +
                                              "如果您是花錢得到的，那麼恭喜您成為盜版遊戲的受害者。" +
                                              "目前遊戲還在開發中，包括戰鬥在內的很多功能尚未實現，請耐心等待。" +
                                              "也歡迎加入Q群與作者聯絡或下載最新版：\\i252315306\\r，" +
                                              "您也可以在B站關注Up主\\i@柒才\\r獲取最新開發信息！另外，全平台都支持摇杆哦~")
            },
            {
                $"{936}_{PalScriptType.Scene}_{1005}_龙葵苏醒2_{3891}_{typeof(ActorPathToCommand)}",
                new ActorPathToCommand(ActorConstants.PlayerActorVirtualID, 288, 20, 0)
            },
            {
                $"{950}_{PalScriptType.Scene}_{1005}_龍葵甦醒2_{3891}_{typeof(ActorPathToCommand)}",
                new ActorPathToCommand(ActorConstants.PlayerActorVirtualID, 288, 20, 0)
            },
            {
                $"{936}_{PalScriptType.Scene}_{1001}_初到镇江_{392}_{typeof(ActorMoveToCommand)}",
                new ActorPathToCommand(1, 318, 142, 0)
            },
            {
                $"{950}_{PalScriptType.Scene}_{1001}_初到鎮江_{392}_{typeof(ActorMoveToCommand)}",
                new ActorPathToCommand(1, 318, 142, 0)
            },
            {
                $"{936}_{PalScriptType.Scene}_{1001}_初到镇江_{688}_{typeof(ActorMoveToCommand)}",
                new ActorPathToCommand(3, 327, 140, 0)
            },
            {
                $"{950}_{PalScriptType.Scene}_{1001}_初到鎮江_{688}_{typeof(ActorMoveToCommand)}",
                new ActorPathToCommand(3, 327, 140, 0)
            },
            {
                $"{936}_{PalScriptType.Scene}_{1001}_初到镇江_{930}_{typeof(ActorMoveToCommand)}",
                new ActorPathToCommand(4, 346, 137, 0)
            },
            {
                $"{950}_{PalScriptType.Scene}_{1001}_初到鎮江_{930}_{typeof(ActorMoveToCommand)}",
                new ActorPathToCommand(4, 346, 137, 0)
            }
        };
    }
}

#endif