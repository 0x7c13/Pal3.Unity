// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Core.Command.SceCommands
{
    #if PAL3
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