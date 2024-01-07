// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2024, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Core.Command.SceCommands
{
    [SceCommand(147, "退出游戏到主菜单")]
    public sealed class GameSwitchToMainMenuCommand : ICommand
    {
        public GameSwitchToMainMenuCommand() {}
    }
}