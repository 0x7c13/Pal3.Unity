// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Core.Command.SceCommands
{
    [SceCommand(66, "取得选择结果并赋值给变量，" +
                    "参数：变量名")]
    public sealed class ScriptVarSetDialogueSelectionResultCommand : ICommand
    {
        public ScriptVarSetDialogueSelectionResultCommand(ushort variable)
        {
            Variable = variable;
        }

        [SceUserVariable] public ushort Variable { get; }
    }
}