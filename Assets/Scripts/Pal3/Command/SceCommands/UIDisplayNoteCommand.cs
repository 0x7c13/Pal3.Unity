// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Command.SceCommands
{
    [AvailableInConsole]
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