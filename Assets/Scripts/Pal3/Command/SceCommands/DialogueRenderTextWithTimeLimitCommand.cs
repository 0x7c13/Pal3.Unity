// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE.txt in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Command.SceCommands
{
    [SceCommand(76, "显示对话，等待用户确定后或者时间结束时脚本继续")]
    public class DialogueRenderTextWithTimeLimitCommand : ICommand
    {
        public DialogueRenderTextWithTimeLimitCommand(string dialogueText)
        {
            DialogueText = dialogueText;
        }

        public string DialogueText { get; }
    }
}