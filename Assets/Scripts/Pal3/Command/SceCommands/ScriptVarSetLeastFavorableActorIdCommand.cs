// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Command.SceCommands
{
    [AvailableInConsole]
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