// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Core.Command.SceCommands
{
    [SceCommand(19, "判断变量是否在两个数之间并赋值给临时变量，" +
                    "参数：变量名，min，max，说明：X>=min并且X<=max?")]
    public class ScriptVarInBetweenCommand : ICommand
    {
        public ScriptVarInBetweenCommand(int variable, int min, int max)
        {
            Variable = variable;
            Min = min;
            Max = max;
        }

        public int Variable { get; }
        public int Min { get; }
        public int Max { get; }
    }
}