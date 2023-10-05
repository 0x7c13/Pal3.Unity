// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Core.Command.SceCommands
{
    #if PAL3A
    [SceCommand(160, "PAL3A捉鬼（龙葵）游戏")]
    public class MiniGameStartGhostHuntingCommand : ICommand
    {
        public MiniGameStartGhostHuntingCommand() { }
    }
    #endif
}