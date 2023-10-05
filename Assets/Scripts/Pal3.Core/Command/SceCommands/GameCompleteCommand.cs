// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Core.Command.SceCommands
{
    #if PAL3A
    [SceCommand(194, "PAL3A游戏结束的最后一个指令，标示游戏通关")]
    public class GameCompleteCommand : ICommand
    {
        public GameCompleteCommand() { }
    }
    #endif
}