// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Core.Command.SceCommands
{
    [SceCommand(130, "变量A减去B，如果减去后A小于零则A取反，并把结算结果重新赋值给变量A" +
                    "参数：变量名A，变量名B", 0b0011)]
    public sealed class ScriptVarDistractAnotherVarCommand : ICommand
    {
        public ScriptVarDistractAnotherVarCommand(ushort variableA, ushort variableB)
        {
            VariableA = variableA;
            VariableB = variableB;
        }

        [SceUserVariable] public ushort VariableA { get; }
        [SceUserVariable] public ushort VariableB { get; }
    }
}