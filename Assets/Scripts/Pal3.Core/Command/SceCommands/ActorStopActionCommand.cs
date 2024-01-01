// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2024, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Core.Command.SceCommands
{
    [SceCommand(221, "ActorPerformActionCommand的循环次数为-1或-2时需要用此命令结束，" +
                    "参数：角色ID")]
    public sealed class ActorStopActionCommand : ICommand
    {
        public ActorStopActionCommand(int actorId)
        {
            ActorId = actorId;
        }

        [SceActorId] public int ActorId { get; set; }
    }
}