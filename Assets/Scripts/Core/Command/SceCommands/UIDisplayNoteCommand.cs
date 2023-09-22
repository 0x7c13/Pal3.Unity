// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Core.Command.SceCommands
{
    [SceCommand(68, "显示提示框信息")]
    public class UIDisplayNoteCommand : ICommand
    {
        public UIDisplayNoteCommand(string note)
        {
            Note = note;
        }

        public string Note { get; }
    }
}