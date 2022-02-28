// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE.txt in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Command.SceCommands
{
    [SceCommand(53, "取得主角对谁的好感最低，" +
                    "参数：变量名")]
    public class ScriptVarSetLeastFavorableActorIdCommand : ICommand
    {
        public ScriptVarSetLeastFavorableActorIdCommand(int variable)
        {
            Variable = variable;
        }

        public int Variable { get; }
    }
}