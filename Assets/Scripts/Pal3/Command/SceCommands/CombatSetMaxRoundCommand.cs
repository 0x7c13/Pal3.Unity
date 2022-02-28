// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE.txt in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Command.SceCommands
{
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