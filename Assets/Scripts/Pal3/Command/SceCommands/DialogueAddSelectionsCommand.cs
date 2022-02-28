// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Command.SceCommands
{
    using System.Collections.Generic;

    [SceCommand(65, "对话选择框，" +
                    "参数：候选项（字符串列表）", isAvailableInConsole: false)]
    public class DialogueAddSelectionsCommand : ICommand
    {
        public DialogueAddSelectionsCommand(List<object> selections)
        {
            Selections = selections;
        }

        public List<object> Selections { get; }
    }
}