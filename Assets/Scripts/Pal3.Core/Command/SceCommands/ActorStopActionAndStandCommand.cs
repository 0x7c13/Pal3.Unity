// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2024, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Core.Command.SceCommands
{
    [SceCommand(71, "使角色结束当前行为，并站立在当前位置，" +
                     "参数：角色ID")]
    public sealed class ActorStopActionAndStandCommand : ICommand
    {
        public ActorStopActionAndStandCommand(int actorId)
        {
            ActorId = actorId;
        }

        [SceActorId] public int ActorId { get; set; }
    }
}