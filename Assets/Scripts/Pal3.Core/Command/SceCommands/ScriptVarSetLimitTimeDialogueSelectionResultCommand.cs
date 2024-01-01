// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2024, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Core.Command.SceCommands
{
    [SceCommand(77, "取得限时选择结果并赋值给变量，" +
                    "参数：变量名")]
    public sealed class ScriptVarSetLimitTimeDialogueSelectionResultCommand : ICommand
    {
        public ScriptVarSetLimitTimeDialogueSelectionResultCommand(ushort variable)
        {
            Variable = variable;
        }

        [SceUserVariable] public ushort Variable { get; }
    }
}