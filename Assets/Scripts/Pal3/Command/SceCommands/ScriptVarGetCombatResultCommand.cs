// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE.txt in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Command.SceCommands
{
    [SceCommand(132, "取得战斗结果（0输1赢）并赋值给变量，" +
                     "参数：变量名")]
    public class ScriptVarGetCombatResultCommand : ICommand
    {
        public ScriptVarGetCombatResultCommand(int variable)
        {
            Variable = variable;
        }

        public int Variable { get; }
    }
}