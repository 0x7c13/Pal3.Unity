// ------------------------------------------------------------------------
//  Copyright (c) 2021-2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

#if PAL3A
namespace Pal3.Core.Command.SceCommands
{
    [SceCommand(197, "设置角色对话时是否保留动作和朝向")]
    public sealed class ActorSetNoTurnCommand : ICommand
    {
        public ActorSetNoTurnCommand(int actorId, int noTurn)
        {
            ActorId = actorId;
            NoTurn = noTurn;
        }

        [SceActorId]
        public int ActorId { get; set; }

        public int NoTurn { get; }
    }
}
#endif
