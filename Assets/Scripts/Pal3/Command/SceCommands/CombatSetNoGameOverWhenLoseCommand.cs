// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Command.SceCommands
{
    [AvailableInConsole]
    [SceCommand(131, "设置战斗失败不显示结束游戏")]
    public class CombatSetNoGameOverWhenLoseCommand : ICommand
    {
        public CombatSetNoGameOverWhenLoseCommand() {}
    }
}