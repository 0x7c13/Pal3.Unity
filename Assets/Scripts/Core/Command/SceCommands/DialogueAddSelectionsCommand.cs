// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Core.Command.SceCommands
{
    using System.Collections.Generic;

    [SceCommand(65, "对话选择框，" +
                    "参数：候选项（字符串列表）")]
    public class DialogueAddSelectionsCommand : ICommand
    {
        public DialogueAddSelectionsCommand(List<object> selections)
        {
            Selections = selections;
        }

        public List<object> Selections { get; }
    }
}