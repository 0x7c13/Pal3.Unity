﻿// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Command.SceCommands
{
    [AvailableInConsole]
    [SceCommand(27, "是否允许玩家控制当前主角，" +
                    "参数：0不可以，1可以")]
    public class PlayerEnableInputCommand : ICommand
    {
        public PlayerEnableInputCommand(int enable)
        {
            Enable = enable;
        }

        public int Enable { get; }
    }
}