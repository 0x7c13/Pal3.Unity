// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2024, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Core.Command.SceCommands
{
    [SceCommand(24, "转动角色面向的方向，" +
                    "参数：角色ID，转过多少角度（-360~360）")]
    public sealed class ActorRotateFacingCommand : ICommand
    {
        public ActorRotateFacingCommand(int actorId, int degrees)
        {
            ActorId = actorId;
            Degrees = degrees;
        }

        [SceActorId] public int ActorId { get; set; }

        public int Degrees { get; }
    }
}