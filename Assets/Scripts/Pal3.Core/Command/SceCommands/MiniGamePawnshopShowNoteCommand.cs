// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2024, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Core.Command.SceCommands
{
    #if PAL3
    [SceCommand(157, "当铺经营游戏显示信息，" +
                     "参数：信息内容")]
    public sealed class MiniGamePawnshopShowNoteCommand : ICommand
    {
        public MiniGamePawnshopShowNoteCommand(string note)
        {
            Note = note;
        }

        public string Note { get; }
    }
    #endif
}