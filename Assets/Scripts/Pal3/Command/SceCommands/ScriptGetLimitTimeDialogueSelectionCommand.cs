// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE.txt in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Command.SceCommands
{
    [SceCommand(77, "取得限时选择结果并赋值给变量，" +
                    "参数：变量名")]
    public class ScriptGetLimitTimeDialogueSelectionCommand : ICommand
    {
        public ScriptGetLimitTimeDialogueSelectionCommand(int variable)
        {
            Variable = variable;
        }

        public int Variable { get; }
    }
}