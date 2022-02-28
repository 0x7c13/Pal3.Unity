// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Command.SceCommands
{
    [SceCommand(10, "A>=B? A Greater or Equal B?",  parameterFlag: 0b0001)]
    public class ScriptVarGreaterThanOrEqualToCommand : ICommand
    {
        public ScriptVarGreaterThanOrEqualToCommand(int variable, int value)
        {
            Variable = variable;
            Value = value;
        }

        public int Variable { get; }
        public int Value { get; }
    }

    [SceCommand(10, "A>=B? A Greater or Equal B?", parameterFlag: 0b0011)]
    public class ScriptCompareUserVarGreaterThanOrEqualToCommand : ICommand
    {
        public ScriptCompareUserVarGreaterThanOrEqualToCommand(int variableA, int variableB)
        {
            VariableA = variableA;
            VariableB = variableB;
        }

        public int VariableA { get; }
        public int VariableB { get; }
    }
}