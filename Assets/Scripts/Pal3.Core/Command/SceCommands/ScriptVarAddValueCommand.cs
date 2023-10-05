// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Core.Command.SceCommands
{
    [SceCommand(14, "给变量增加值，Var += Value，" +
                    "参数：变量名（ID），值")]
    public class ScriptVarAddValueCommand : ICommand
    {
        public ScriptVarAddValueCommand(int variable, int value)
        {
            Variable = variable;
            Value = value;
        }

        public int Variable { get; }
        public int Value { get; }
    }
}