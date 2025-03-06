// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2025, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Core.Command.SceCommands
{
    #if PAL3A
    [SceCommand(192, "南宫煌以变身状态下进入Boss战斗")]
    public sealed class CombatEnterBossFightUsingMusicWithSpecialActorCommand : ICommand
    {
        public CombatEnterBossFightUsingMusicWithSpecialActorCommand(
            uint monster1Id,
            uint monster2Id,
            uint monster3Id,
            uint monster4Id,
            uint monster5Id,
            uint monster6Id,
            string combatMusicName,
            int combatCommandId)
        {
            Monster1Id = monster1Id;
            Monster2Id = monster2Id;
            Monster3Id = monster3Id;
            Monster4Id = monster4Id;
            Monster5Id = monster5Id;
            Monster6Id = monster6Id;
            CombatMusicName = combatMusicName;
            CombatCommandId = combatCommandId;
        }

        public uint Monster1Id { get; }
        public uint Monster2Id { get; }
        public uint Monster3Id { get; }
        public uint Monster4Id { get; }
        public uint Monster5Id { get; }
        public uint Monster6Id { get; }
        public string CombatMusicName { get; }
        public int CombatCommandId { get; }
    }
    #endif
}