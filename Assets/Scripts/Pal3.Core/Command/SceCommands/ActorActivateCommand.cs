// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2024, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Core.Command.SceCommands
{
    [SceCommand(28, "设置角色是否激活，" +
                    "参数：角色ID，是否激活（1是0否）")]
    public sealed class ActorActivateCommand : ICommand
    {
        public ActorActivateCommand(int actorId, int isActive)
        {
            ActorId = actorId;
            IsActive = isActive;
        }

        [SceActorId] public int ActorId { get; set; }
        public int IsActive { get; }
    }
}