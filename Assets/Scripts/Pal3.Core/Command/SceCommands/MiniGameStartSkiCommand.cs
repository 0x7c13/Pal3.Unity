// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2025, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Core.Command.SceCommands
{
    #if PAL3
    [SceCommand(107, "进入滑雪游戏，" +
                     "参数：游戏结束后执行的脚本ID")]
    public sealed class MiniGameStartSkiCommand : ICommand
    {
        public MiniGameStartSkiCommand(int endGameScriptId)
        {
            EndGameScriptId = endGameScriptId;
        }

        public int EndGameScriptId { get; }
    }
    #endif
}