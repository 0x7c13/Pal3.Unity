// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2025, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Core.Command.SceCommands
{
    [SceCommand(13, "给变量赋值，Var <== Value，" +
                    "参数：变量名（ID），值")]
    public sealed class ScriptVarSetValueCommand : ICommand
    {
        public ScriptVarSetValueCommand(ushort variable, int value)
        {
            Variable = variable;
            Value = value;
        }

        [SceUserVariable] public ushort Variable { get; }
        public int Value { get; }
    }
}