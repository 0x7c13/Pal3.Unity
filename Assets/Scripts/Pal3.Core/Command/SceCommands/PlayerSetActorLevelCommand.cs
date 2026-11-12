// ------------------------------------------------------------------------
//  Copyright (c) 2021-2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

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
    [SceCommand(145, "将角色等级提升到其他在队角色的平均等级，不降低原等级")]
    public sealed class PlayerSetActorLevelCommand : ICommand
    {
        public PlayerSetActorLevelCommand(int actorId) { ActorId = actorId; }
        [SceActorId] public int ActorId { get; set; }
    }
    #endif
}