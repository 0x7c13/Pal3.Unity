// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Command.SceCommands
{
    [AvailableInConsole]
    [SceCommand(12, "如果当前计算值为false的话就使脚本跳转至Offset处运行，" +
                    "参数：当前运行脚本的数据offset偏移值位置")]
    public class ScriptTestGotoCommand : ICommand
    {
        public ScriptTestGotoCommand(int offset)
        {
            Offset = offset;
        }

        public int Offset { get; }
    }
}