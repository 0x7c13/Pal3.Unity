// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Core.Command.SceCommands
{
    [SceCommand(65, "对话选择框，" +
                    "参数：候选项（字符串列表）")]
    public class DialogueAddSelectionsCommand : ICommand
    {
        public DialogueAddSelectionsCommand(object[] selections)
        {
            Selections = selections;
        }

        public object[] Selections { get; }
    }
}