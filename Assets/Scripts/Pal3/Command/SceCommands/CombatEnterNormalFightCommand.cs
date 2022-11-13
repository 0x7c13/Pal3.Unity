// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Command.SceCommands
{
    [AvailableInConsole]
    [SceCommand(73, "进入普通战斗")]
    public class CombatEnterNormalFightCommand : ICommand
    {
        public CombatEnterNormalFightCommand(
            int numberOfMonster,
            int monster1Id,
            int monster2Id,
            int monster3Id)
        {
            NumberOfMonster = numberOfMonster;
            Monster1Id = monster1Id;
            Monster2Id = monster2Id;
            Monster3Id = monster3Id;
        }

        public int NumberOfMonster { get; }
        public int Monster1Id { get; }
        public int Monster2Id { get; }
        public int Monster3Id { get; }
    }
}