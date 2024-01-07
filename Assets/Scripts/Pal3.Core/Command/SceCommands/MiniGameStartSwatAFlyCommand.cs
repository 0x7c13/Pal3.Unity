// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2024, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Core.Command.SceCommands
{
    #if PAL3
    [SceCommand(105, "进入打苍蝇游戏")]
    public sealed class MiniGameStartSwatAFlyCommand : ICommand
    {
        public MiniGameStartSwatAFlyCommand() {}
    }
    #endif
}