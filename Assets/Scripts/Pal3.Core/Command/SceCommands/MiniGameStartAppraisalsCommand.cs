// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2024, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Core.Command.SceCommands
{
    #if PAL3
    [SceCommand(104, "进入签定游戏")]
    public sealed class MiniGameStartAppraisalsCommand : ICommand
    {
        public MiniGameStartAppraisalsCommand() {}
    }
    #endif
}