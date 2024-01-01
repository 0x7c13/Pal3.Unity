// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2024, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Core.Command.SceCommands
{
    [SceCommand(9, "判断变量是否不等于给定值并与临时变量计算结果")]
    public sealed class ScriptEvaluateVarIsNotEqualToCommand : ICommand
    {
        public ScriptEvaluateVarIsNotEqualToCommand(ushort variable, int value)
        {
            Variable = variable;
            Value = value;
        }

        [SceUserVariable] public ushort Variable { get; }
        public int Value { get; }
    }
}