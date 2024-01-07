// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2024, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Core.Command.SceCommands
{
    [SceCommand(131, "设置战斗失败不显示结束游戏")]
    public sealed class CombatSetNoGameOverWhenLoseCommand : ICommand
    {
        public CombatSetNoGameOverWhenLoseCommand() {}
    }
}