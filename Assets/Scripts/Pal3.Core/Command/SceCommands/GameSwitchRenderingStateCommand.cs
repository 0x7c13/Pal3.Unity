﻿// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2024, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Core.Command.SceCommands
{
    [SceCommand(102, "选择当前渲染状态")]
    public sealed class GameSwitchRenderingStateCommand : ICommand
    {
        public GameSwitchRenderingStateCommand(int state)
        {
            State = state;
        }

        public int State { get; }
    }
}