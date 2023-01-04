// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Command.SceCommands
{
    #if PAL3
    [AvailableInConsole]
    [SceCommand(104, "进入签定游戏")]
    public class MiniGameStartAppraisalsCommand : ICommand
    {
        public MiniGameStartAppraisalsCommand() {}
    }
    #endif
}