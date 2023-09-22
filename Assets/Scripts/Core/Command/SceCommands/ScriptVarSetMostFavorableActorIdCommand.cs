// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Core.Command.SceCommands
{
    [SceCommand(52, "取得主角对谁的好感最高，" +
                    "参数：变量名")]
    public class ScriptVarSetMostFavorableActorIdCommand : ICommand
    {
        public ScriptVarSetMostFavorableActorIdCommand(int variable)
        {
            Variable = variable;
        }

        public int Variable { get; }
    }
}