// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Core.Command.SceCommands
{
    [SceCommand(11, "判断变量是否小于等于给定值并与临时变量计算结果", 0b0001)]
    public class ScriptEvaluateVarIsLessThanOrEqualToCommand : ICommand
    {
        public ScriptEvaluateVarIsLessThanOrEqualToCommand(ushort variable, int value)
        {
            Variable = variable;
            Value = value;
        }

        public ushort Variable { get; }
        public int Value { get; }
    }
}