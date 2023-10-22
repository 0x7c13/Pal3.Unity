// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Game.Command.Extensions
{
    using Core.Command;

    [AvailableInConsole]
    public sealed class ActorSetFacingCommand : ICommand
    {
        public ActorSetFacingCommand(int actorId, int degrees)
        {
            ActorId = actorId;
            Degrees = degrees;
        }

        [SceActorId] public int ActorId { get; set; }
        public int Degrees { get; }
    }
}