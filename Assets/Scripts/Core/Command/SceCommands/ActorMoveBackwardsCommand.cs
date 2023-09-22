// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Core.Command.SceCommands
{
    [SceCommand(208, "角色向后平移，" +
                     "参数：角色ID，距离（原GameBox引擎下的距离单位）")]
    public class ActorMoveBackwardsCommand : ICommand
    {
        public ActorMoveBackwardsCommand(int actorId, float gameBoxDistance)
        {
            ActorId = actorId;
            GameBoxDistance = gameBoxDistance;
        }

        // 角色ID为-1时 (byte值就是255) 表示当前受玩家操作的主角
        public int ActorId { get; }
        public float GameBoxDistance { get; }
    }
}