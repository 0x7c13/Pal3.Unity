// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Command.SceCommands
{
    [AvailableInConsole]
    [SceCommand(5, "指定逻辑指令与标志变量的操作，" +
                   "参数：0：赋值/替换，1：与，2：或")]
    public class ScriptSetOperatorCommand : ICommand
    {
        public ScriptSetOperatorCommand(int operatorType)
        {
            OperatorType = operatorType;
        }

        public int OperatorType { get; }
    }
}