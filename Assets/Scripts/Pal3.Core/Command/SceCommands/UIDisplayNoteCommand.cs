// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2025, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Core.Command.SceCommands
{
    [SceCommand(68, "显示提示框信息")]
    public sealed class UIDisplayNoteCommand : ICommand
    {
        public UIDisplayNoteCommand(string note)
        {
            Note = note;
        }

        public string Note { get; }
    }
}