﻿// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2024, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Core.Command.SceCommands
{
    #if PAL3
    [SceCommand(145, "设置目标角色等级（所有当前角色等级的平均值+增加值），" +
                    "参数：角色ID，等级增加值")]
    public sealed class PlayerSetActorLevelCommand : ICommand
    {
        public PlayerSetActorLevelCommand(int actorId, int levelIncrease)
        {
            ActorId = actorId;
            LevelIncrease = levelIncrease;
        }

        [SceActorId] public int ActorId { get; set; }
        public int LevelIncrease { get; }
    }
    #elif PAL3A
    [SceCommand(145, "设置目标角色等级（所有当前角色等级的平均值+增加值），" +
                     "参数：角色ID，等级增加值")]
    public sealed class PlayerSetActorLevelCommand : ICommand
    {
        public PlayerSetActorLevelCommand(int actorId, int unknown1, int unknown2, int levelIncrease)
        {
            ActorId = actorId;
            Unknown1 = unknown1;
            Unknown2 = unknown2;
            LevelIncrease = levelIncrease;
        }

        [SceActorId] public int ActorId { get; set; }
        public int Unknown1 { get; }
        public int Unknown2 { get; }
        public int LevelIncrease { get; }
    }
    #endif
}