// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Command.SceCommands
{
    [SceCommand(2, "设置脚本运行模式，" +
                   "参数：1为单步运行，2为连续执行")]
    public class ScriptChangeExecutionModeCommand : ICommand
    {
        public ScriptChangeExecutionModeCommand(int mode)
        {
            Mode = mode;
        }

        public int Mode { get; }
    }
}