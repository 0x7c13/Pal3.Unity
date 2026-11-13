// ------------------------------------------------------------------------
//  Copyright (c) 2021-2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

#if PAL3A
namespace Pal3.Core.Command.SceCommands
{
    [SceCommand(166, "按装备武器类别播放攻击动作")]
    public sealed class ActorPerformAttackCommand : ICommand
    {
        public ActorPerformAttackCommand(int actorId, int loopCount)
        {
            ActorId = actorId;
            LoopCount = loopCount;
        }

        [SceActorId]
        public int ActorId { get; set; }

        public int LoopCount { get; }
    }
}
#endif
