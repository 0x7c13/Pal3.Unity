// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Command.SceCommands
{
    [AvailableInConsole]
    [SceCommand(66, "取得选择结果并赋值给变量，" +
                    "参数：变量名")]
    public class ScriptGetDialogueSelectionCommand : ICommand
    {
        public ScriptGetDialogueSelectionCommand(int variable)
        {
            Variable = variable;
        }

        public int Variable { get; }
    }
}