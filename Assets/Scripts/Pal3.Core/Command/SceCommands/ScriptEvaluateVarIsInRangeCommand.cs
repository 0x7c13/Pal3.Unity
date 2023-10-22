// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Core.Command.SceCommands
{
    [SceCommand(19, "判断变量值是否在给定区间范围内并与临时变量计算结果")]
    public sealed class ScriptEvaluateVarIsInRangeCommand : ICommand
    {
        public ScriptEvaluateVarIsInRangeCommand(ushort variable,
            int min,
            int max)
        {
            Variable = variable;
            Min = min;
            Max = max;
        }

        [SceUserVariable] public ushort Variable { get; }
        public int Min { get; }
        public int Max { get; }
    }
}