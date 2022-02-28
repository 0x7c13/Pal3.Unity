// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Command.SceCommands
{
    [SceCommand(80, "进入Boss战")]
    public class CombatEnterBossFightCommand : ICommand
    {
        public CombatEnterBossFightCommand(
            int monster1Id,
            int monster2Id,
            int monster3Id,
            int monster4Id,
            int monster5Id,
            int monster6Id)
        {
            Monster1Id = monster1Id;
            Monster2Id = monster2Id;
            Monster3Id = monster3Id;
            Monster4Id = monster4Id;
            Monster5Id = monster5Id;
            Monster6Id = monster6Id;
        }

        public int Monster1Id { get; }
        public int Monster2Id { get; }
        public int Monster3Id { get; }
        public int Monster4Id { get; }
        public int Monster5Id { get; }
        public int Monster6Id { get; }
    }
}