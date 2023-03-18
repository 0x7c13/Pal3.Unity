﻿// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Command.SceCommands
{
    [AvailableInConsole]
    [SceCommand(12, "如果当前计算值为true的话就继续执行下一行指令，否则使脚本跳转至Offset处运行，" +
                    "参数：条件判定失败时所执行的脚本数据offset偏移值位置")]
    public class ScriptGotoIfConditionFailedCommand : ICommand
    {
        public ScriptGotoIfConditionFailedCommand(int offset)
        {
            Offset = offset;
        }

        public int Offset { get; }
    }
}