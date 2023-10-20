// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Core.Command.SceCommands
{
    [SceCommand(2, "设置脚本运行模式，" +
                   "参数：1为异步执行，2为同步（顺序）执行")]
    public class ScriptRunnerChangeExecutionModeCommand : ICommand
    {
        public ScriptRunnerChangeExecutionModeCommand(int mode)
        {
            Mode = mode;
        }

        public int Mode { get; }
    }
}