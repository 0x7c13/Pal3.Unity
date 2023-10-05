// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Core.Command.SceCommands
{
    [SceCommand(178, "给角色设置Y坐标" +
                     "参数：角色ID，Y坐标（原GameBox引擎下的Y坐标单位）")]
    public class ActorSetYPositionCommand : ICommand
    {
        public ActorSetYPositionCommand(
            int actorId,
            float gameBoxYPosition)
        {
            ActorId = actorId;
            GameBoxYPosition = gameBoxYPosition;
        }

        public int ActorId { get; }
        public float GameBoxYPosition { get; }
    }
}