// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Core.Command.SceCommands
{
    [SceCommand(202, "设置某个主角是否在队伍中出现，" +
                     "参数：主角ID，是否出现（1出现0不出现）")]
    public sealed class TeamAddOrRemoveActorCommand : ICommand
    {
        public TeamAddOrRemoveActorCommand(int actorId, int isIn)
        {
            ActorId = actorId;
            IsIn = isIn;
        }

        [SceUserVariable] public int ActorId { get; }
        public int IsIn { get; }
    }
}