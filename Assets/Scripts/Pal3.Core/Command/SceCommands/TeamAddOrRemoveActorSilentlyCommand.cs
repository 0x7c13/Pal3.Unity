// ------------------------------------------------------------------------
//  Copyright (c) 2021-2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

#if PAL3A
namespace Pal3.Core.Command.SceCommands
{
    [SceCommand(193, "静默加入或离开队伍")]
    public sealed class TeamAddOrRemoveActorSilentlyCommand : ICommand
    {
        public TeamAddOrRemoveActorSilentlyCommand(int actorId, int isIn)
        {
            ActorId = actorId;
            IsIn = isIn;
        }

        [SceActorId]
        public int ActorId { get; set; }

        public int IsIn { get; }
    }
}
#endif
