// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2025, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Core.Command.SceCommands
{
    [SceCommand(3, "使脚本跳转到指定位置，" +
                    "参数：脚本数据offset偏移值位置")]
    public sealed class ScriptRunnerGotoCommand : ICommand
    {
        public ScriptRunnerGotoCommand(int offset)
        {
            Offset = offset;
        }

        public int Offset { get; }
    }
}