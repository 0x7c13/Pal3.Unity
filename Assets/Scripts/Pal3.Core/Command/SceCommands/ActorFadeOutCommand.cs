// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2024, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Core.Command.SceCommands
{
    [SceCommand(135, "角色淡出")]
    public sealed class ActorFadeOutCommand : ICommand
    {
        public ActorFadeOutCommand(int actorId)
        {
            ActorId = actorId;
        }

        [SceActorId] public int ActorId { get; set; }
    }
}