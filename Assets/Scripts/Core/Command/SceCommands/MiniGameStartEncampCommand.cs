// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Core.Command.SceCommands
{
    #if PAL3
    [SceCommand(106, "进入宿营游戏")]
    public class MiniGameStartEncampCommand : ICommand
    {
        public MiniGameStartEncampCommand(int flag)
        {
            Flag = flag;
        }

        public int Flag { get; }
    }
    #endif
}