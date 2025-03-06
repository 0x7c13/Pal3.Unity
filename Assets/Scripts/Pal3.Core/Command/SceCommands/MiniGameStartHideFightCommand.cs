// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2025, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Core.Command.SceCommands
{
    #if PAL3
    [SceCommand(113, "使当前场景进入<当铺&刺使府夜间特殊战斗关>状态")]
    public sealed class MiniGameStartHideFightCommand : ICommand
    {
        public MiniGameStartHideFightCommand() {}
    }
    #endif
}