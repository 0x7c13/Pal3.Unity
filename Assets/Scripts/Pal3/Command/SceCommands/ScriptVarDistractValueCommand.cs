// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Command.SceCommands
{
    [AvailableInConsole]
    [SceCommand(130, "变量A减去B，如果减去后A小于零则A取反，" +
                    "参数：变量名A，变量名B")]
    public class ScriptVarDistractValueCommand : ICommand
    {
        public ScriptVarDistractValueCommand(int variableA, int variableB)
        {
            VariableA = variableA;
            VariableB = variableB;
        }

        public int VariableA { get; }
        public int VariableB { get; }
    }
}