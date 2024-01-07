// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2024, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Core.Command.SceCommands
{
    [SceCommand(10, "判断变量是否大于等于给定值并与临时变量计算结果")]
    public sealed class ScriptEvaluateVarIsGreaterThanOrEqualToCommand : ICommand
    {
        public ScriptEvaluateVarIsGreaterThanOrEqualToCommand(
            ushort variable,
            int value)
        {
            Variable = variable;
            Value = value;
        }

        [SceUserVariable] public ushort Variable { get; }
        public int Value { get; }
    }

    [SceCommand(10, "判断变量A的值是否大于等于变量B的值并与临时变量计算结果")]
    public sealed class ScriptEvaluateVarIsGreaterThanOrEqualToAnotherVarCommand : ICommand
    {
        public ScriptEvaluateVarIsGreaterThanOrEqualToAnotherVarCommand(
            ushort variableA,
            ushort variableB)
        {
            VariableA = variableA;
            VariableB = variableB;
        }

        [SceUserVariable] public ushort VariableA { get; }
        [SceUserVariable] public ushort VariableB { get; }
    }
}