// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2025, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Core.Command.SceCommands
{
    [SceCommand(62, "显示对话，等待用户确定后脚本继续")]
    public sealed class DialogueRenderTextCommand : ICommand
    {
        public DialogueRenderTextCommand(string dialogueText)
        {
            DialogueText = dialogueText;
        }

        public string DialogueText { get; }
    }
}