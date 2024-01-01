// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2024, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Core.Command.SceCommands
{
    [SceCommand(209, "设定一个角色面向另一个角色（的位置），" +
                     "参数：被设置的角色的ID，面向的角色的ID")]
    public sealed class ActorLookAtActorCommand : ICommand
    {
        public ActorLookAtActorCommand(int actorId, int lookAtActorId)
        {
            ActorId = actorId;
            LookAtActorId = lookAtActorId;
        }

        [SceActorId] public int ActorId { get; set; }

        [SceActorId] public int LookAtActorId { get; set; }
    }
}