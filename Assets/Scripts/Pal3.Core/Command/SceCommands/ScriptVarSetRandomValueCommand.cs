// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Core.Command.SceCommands
{
    [SceCommand(17, "给变量随机赋值一个正整数，" +
                    "参数：输出变量，随机值上限（最大值但不包括）；输出0到最大值之间的一个整数", 0b0001)]
    public class ScriptVarSetRandomValueCommand : ICommand
    {
        public ScriptVarSetRandomValueCommand(ushort variable, int maxExclusiveValue)
        {
            Variable = variable;
            MaxExclusiveValue = maxExclusiveValue;
        }

        public ushort Variable { get; }
        public int MaxExclusiveValue { get; }
    }
}