// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Core.Command.SceCommands
{
    [SceCommand(10, "判断变量是否大于等于给定值并与临时变量计算结果", 0b0001)]
    public class ScriptEvaluateVarIsGreaterThanOrEqualToCommand : ICommand
    {
        public ScriptEvaluateVarIsGreaterThanOrEqualToCommand(ushort variable, int value)
        {
            Variable = variable;
            Value = value;
        }

        public ushort Variable { get; }
        public int Value { get; }
    }

    [SceCommand(10, "变量A的值>=变量B的值", 0b0011)]
    public class ScriptEvaluateVarIsGreaterThanOrEqualToAnotherVarCommand : ICommand
    {
        public ScriptEvaluateVarIsGreaterThanOrEqualToAnotherVarCommand(ushort variableA, ushort variableB)
        {
            VariableA = variableA;
            VariableB = variableB;
        }

        public ushort VariableA { get; }
        public ushort VariableB { get; }
    }
}