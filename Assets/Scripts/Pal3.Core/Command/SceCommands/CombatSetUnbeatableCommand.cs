// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2025, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Core.Command.SceCommands
{
    [SceCommand(83, "设置战斗为必败")]
    public sealed class CombatSetUnbeatableCommand : ICommand
    {
        public CombatSetUnbeatableCommand() {}
    }
}