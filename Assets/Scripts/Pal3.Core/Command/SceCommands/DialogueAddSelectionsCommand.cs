﻿// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2024, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Core.Command.SceCommands
{
    [SceCommand(65, "对话选择框，" +
                    "参数：候选项（字符串列表）")]
    public sealed class DialogueAddSelectionsCommand : ICommand
    {
        public DialogueAddSelectionsCommand(object[] selections)
        {
            Selections = selections;
        }

        public object[] Selections { get; }
    }
}