// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Command.SceCommands
{
    [AvailableInConsole]
    [SceCommand(82, "设置战斗的最大回合数")]
    public class CombatSetMaxRoundCommand : ICommand
    {
        public CombatSetMaxRoundCommand(int maxRound)
        {
            MaxRound = maxRound;
        }

        public int MaxRound { get; }
    }
}