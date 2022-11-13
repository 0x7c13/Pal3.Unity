// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Command.SceCommands
{
    [AvailableInConsole]
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