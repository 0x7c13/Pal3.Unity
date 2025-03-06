// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2025, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Core.Command.SceCommands
{
    [SceCommand(5, "指定逻辑指令与标志变量的操作，" +
                   "参数：0：赋值/替换，1：与，2：或")]
    public sealed class ScriptRunnerSetOperatorCommand : ICommand
    {
        public ScriptRunnerSetOperatorCommand(int operatorType)
        {
            OperatorType = operatorType;
        }

        public int OperatorType { get; }
    }
}