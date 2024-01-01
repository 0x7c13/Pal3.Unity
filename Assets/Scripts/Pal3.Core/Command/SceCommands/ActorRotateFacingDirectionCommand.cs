// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2024, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Core.Command.SceCommands
{
    [SceCommand(210, "转动角色的方向，" +
                     "参数：ID，方向（方向值为0-7）")]
    public sealed class ActorRotateFacingDirectionCommand : ICommand
    {
        public ActorRotateFacingDirectionCommand(int actorId, int direction)
        {
            ActorId = actorId;
            Direction = direction;
        }

        [SceActorId] public int ActorId { get; set; }

        public int Direction { get; }
    }
}