// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Command.SceCommands
{
    #if PAL3
    [AvailableInConsole]
    [SceCommand(107, "进入滑雪游戏，" +
                     "参数：游戏结束后执行的脚本ID")]
    public class MiniGameStartSkiCommand : ICommand
    {
        public MiniGameStartSkiCommand(int endGameScriptId)
        {
            EndGameScriptId = endGameScriptId;
        }

        public int EndGameScriptId { get; }
    }
    #endif
}