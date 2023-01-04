// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Command.SceCommands
{
    #if PAL3
    [AvailableInConsole]
    [SceCommand(105, "进入打苍蝇游戏")]
    public class MiniGameStartSwatAFlyCommand : ICommand
    {
        public MiniGameStartSwatAFlyCommand() {}
    }
    #endif
}