// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Command.SceCommands
{
    #if PAL3
    [AvailableInConsole]
    [SceCommand(157, "当铺经营游戏显示信息，" +
                     "参数：信息内容")]
    public class MiniGamePawnshopShowNoteCommand : ICommand
    {
        public MiniGamePawnshopShowNoteCommand(string note)
        {
            Note = note;
        }

        public string Note { get; }
    }
    #endif
}