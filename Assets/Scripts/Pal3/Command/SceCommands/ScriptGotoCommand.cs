﻿// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Command.SceCommands
{
    [AvailableInConsole]
    [SceCommand(3, "使脚本跳转到指定位置，" +
                    "参数：脚本数据offset偏移值位置")]
    public class ScriptGotoCommand : ICommand
    {
        public ScriptGotoCommand(int offset)
        {
            Offset = offset;
        }

        public int Offset { get; }
    }
}