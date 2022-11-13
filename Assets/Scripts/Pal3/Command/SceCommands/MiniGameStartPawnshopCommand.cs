// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Command.SceCommands
{
    #if PAL3
    [AvailableInConsole]
    [SceCommand(103, "进入当铺经营游戏，" +
                     "参数：当铺经营脚本ID")]
    public class MiniGameStartPawnshopCommand : ICommand
    {
        public MiniGameStartPawnshopCommand(int scriptId)
        {
            ScriptId = scriptId;
        }

        public int ScriptId { get; }
    }
    #endif
}