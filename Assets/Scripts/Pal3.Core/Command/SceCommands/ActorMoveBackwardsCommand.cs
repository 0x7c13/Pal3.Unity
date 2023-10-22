// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Core.Command.SceCommands
{
    [SceCommand(208, "角色向后平移，" +
                     "参数：角色ID，距离（原GameBox引擎下的距离单位）")]
    public sealed class ActorMoveBackwardsCommand : ICommand
    {
        public ActorMoveBackwardsCommand(int actorId, float gameBoxDistance)
        {
            ActorId = actorId;
            GameBoxDistance = gameBoxDistance;
        }

        [SceActorId] public int ActorId { get; set; }
        public float GameBoxDistance { get; }
    }
}