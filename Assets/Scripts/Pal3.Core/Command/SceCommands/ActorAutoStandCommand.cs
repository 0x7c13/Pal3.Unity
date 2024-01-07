// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2024, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Core.Command.SceCommands
{
    [SceCommand(207, "指定角色是否在PerformAction指令完成后自动切换成站立的动作，" +
                     "参数：角色ID，是否自动站立（1是0否）")]
    public sealed class ActorAutoStandCommand : ICommand
    {
        public ActorAutoStandCommand(int actorId, int autoStand)
        {
            ActorId = actorId;
            AutoStand = autoStand;
        }

        [SceActorId] public int ActorId { get; set; }
        public int AutoStand { get; }
    }
}