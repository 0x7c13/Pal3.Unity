// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Core.Command.SceCommands
{
    [SceCommand(80, "进入Boss战")]
    public sealed class CombatEnterBossFightCommand : ICommand
    {
        public CombatEnterBossFightCommand(
            uint monster1Id,
            uint monster2Id,
            uint monster3Id,
            uint monster4Id,
            uint monster5Id,
            uint monster6Id)
        {
            Monster1Id = monster1Id;
            Monster2Id = monster2Id;
            Monster3Id = monster3Id;
            Monster4Id = monster4Id;
            Monster5Id = monster5Id;
            Monster6Id = monster6Id;
        }

        public uint Monster1Id { get; }
        public uint Monster2Id { get; }
        public uint Monster3Id { get; }
        public uint Monster4Id { get; }
        public uint Monster5Id { get; }
        public uint Monster6Id { get; }
    }
}