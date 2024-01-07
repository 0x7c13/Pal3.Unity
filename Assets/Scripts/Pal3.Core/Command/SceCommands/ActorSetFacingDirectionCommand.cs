// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2024, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Core.Command.SceCommands
{
    [SceCommand(23, "瞬间使角色面向的方向改变，" +
                    "参数：角色ID，方向（方向值为0-7）")]
    public sealed class ActorSetFacingDirectionCommand : ICommand
    {
        public ActorSetFacingDirectionCommand(int actorId, int direction)
        {
            ActorId = actorId;
            Direction = direction;
        }

        [SceActorId] public int ActorId { get; set; }

        public int Direction { get; }
    }
}