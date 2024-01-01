// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2024, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Core.Command.SceCommands
{
    [SceCommand(136, "角色淡入")]
    public sealed class ActorFadeInCommand : ICommand
    {
        public ActorFadeInCommand(int actorId)
        {
            ActorId = actorId;
        }

        [SceActorId] public int ActorId { get; set; }
    }
}