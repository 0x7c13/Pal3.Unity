// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Core.Command.SceCommands
{
    [SceCommand(73, "进入普通战斗")]
    public sealed class CombatEnterNormalFightCommand : ICommand
    {
        public CombatEnterNormalFightCommand(
            uint numberOfMonster,
            uint monster1Id,
            uint monster2Id,
            uint monster3Id)
        {
            NumberOfMonster = numberOfMonster;
            Monster1Id = monster1Id;
            Monster2Id = monster2Id;
            Monster3Id = monster3Id;
        }

        public uint NumberOfMonster { get; }
        public uint Monster1Id { get; }
        public uint Monster2Id { get; }
        public uint Monster3Id { get; }
    }
}