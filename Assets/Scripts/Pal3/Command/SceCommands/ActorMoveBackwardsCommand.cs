// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Command.SceCommands
{
    [SceCommand(208, "角色向后平移，" +
                     "参数：角色ID，距离")]
    public class ActorMoveBackwardsCommand : ICommand
    {
        public ActorMoveBackwardsCommand(int actorId, float distance)
        {
            ActorId = actorId;
            Distance = distance;
        }

        // 角色ID为-1时 (byte值就是255) 表示当前受玩家操作的主角
        public int ActorId { get; }
        public float Distance { get; }
    }
}