// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Command.SceCommands
{
    #if PAL3A
    [SceCommand(195, "进入Boss战斗？")]
    public class UnknownCommand195 : ICommand
    {
        public UnknownCommand195(
            int monster1Id,
            int monster2Id,
            int monster3Id,
            int monster4Id,
            int monster5Id,
            int monster6Id,
            string combatMusic)
        {
            Monster1Id = monster1Id;
            Monster2Id = monster2Id;
            Monster3Id = monster3Id;
            Monster4Id = monster4Id;
            Monster5Id = monster5Id;
            Monster6Id = monster6Id;
            CombatMusic = combatMusic;
        }

        public int Monster1Id { get; }
        public int Monster2Id { get; }
        public int Monster3Id { get; }
        public int Monster4Id { get; }
        public int Monster5Id { get; }
        public int Monster6Id { get; }
        public string CombatMusic { get; }
    }
    #endif
}