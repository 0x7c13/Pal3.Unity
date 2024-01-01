// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2024, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Core.Command.SceCommands
{
    [SceCommand(204, "设置玩家控制哪个主角，" +
                    "参数：主角ID")]
    public sealed class ActorEnablePlayerControlCommand : ICommand
    {
        public ActorEnablePlayerControlCommand(int actorId)
        {
            ActorId = actorId;
        }

        [SceActorId] public int ActorId { get; set; }
    }
}