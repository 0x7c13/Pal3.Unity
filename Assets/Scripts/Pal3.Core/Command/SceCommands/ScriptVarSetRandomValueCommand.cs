// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Core.Command.SceCommands
{
    [SceCommand(17, "给变量随机赋值一个正整数，" +
                    "参数：输出变量，随机值上限（最大值）；输出0到最大值之间的一个整数")]
    public class ScriptVarSetRandomValueCommand : ICommand
    {
        public ScriptVarSetRandomValueCommand(int variable, int maxValue)
        {
            Variable = variable;
            MaxValue = maxValue;
        }

        public int Variable { get; }
        public int MaxValue { get; }
    }
}