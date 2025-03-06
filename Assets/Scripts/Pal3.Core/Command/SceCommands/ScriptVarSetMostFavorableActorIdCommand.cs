// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2025, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Core.Command.SceCommands
{
    [SceCommand(52, "取得主角对谁的好感最高并赋值给变量，" +
                    "参数：变量名")]
    public sealed class ScriptVarSetMostFavorableActorIdCommand : ICommand
    {
        public ScriptVarSetMostFavorableActorIdCommand(ushort variable)
        {
            Variable = variable;
        }

        [SceUserVariable] public ushort Variable { get; }
    }
}