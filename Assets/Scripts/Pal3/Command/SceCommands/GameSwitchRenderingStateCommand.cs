﻿// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Command.SceCommands
{
    [AvailableInConsole]
    [SceCommand(102, "选择当前渲染状态")]
    public class GameSwitchRenderingStateCommand : ICommand
    {
        public GameSwitchRenderingStateCommand(int state)
        {
            State = state;
        }

        public int State { get; }
    }
}