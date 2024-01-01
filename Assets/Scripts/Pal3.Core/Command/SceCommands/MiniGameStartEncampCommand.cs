// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2024, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Core.Command.SceCommands
{
    #if PAL3
    [SceCommand(106, "进入宿营游戏")]
    public sealed class MiniGameStartEncampCommand : ICommand
    {
        public MiniGameStartEncampCommand(int flag)
        {
            Flag = flag;
        }

        public int Flag { get; }
    }
    #endif
}