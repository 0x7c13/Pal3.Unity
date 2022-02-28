// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE.txt in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Command.SceCommands
{
    [SceCommand(145, "设置目标角色等级（所有当前角色等级的平均值+增加值），" +
                    "参数：角色ID，等级增加值")]
    public class PlayerSetActorLevelCommand : ICommand
    {
        public PlayerSetActorLevelCommand(int actorId, int levelIncrease)
        {
            ActorId = actorId;
            LevelIncrease = levelIncrease;
        }

        public int ActorId { get; }
        public int LevelIncrease { get; }
    }
}