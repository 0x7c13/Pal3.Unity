// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Core.Command.SceCommands
{
    [SceCommand(49, "取出当前金钱数并赋值给变量，" +
                    "参数：变量名")]
    public class ScriptGetMoneyCommand : ICommand
    {
        public ScriptGetMoneyCommand(int variable)
        {
            Variable = variable;
        }

        public int Variable { get; }
    }
}