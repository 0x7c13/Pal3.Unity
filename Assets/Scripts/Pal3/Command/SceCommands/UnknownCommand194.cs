// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Command.SceCommands
{
    #if PAL3A
    [SceCommand(194, "PAL3游戏结束的最后一个指令，目测是播放制作人信息???")]
    public class UnknownCommand194 : ICommand
    {
        public UnknownCommand194() { }
    }
    #endif
}