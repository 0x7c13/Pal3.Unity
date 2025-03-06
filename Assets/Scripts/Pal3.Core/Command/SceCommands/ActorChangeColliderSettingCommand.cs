// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2025, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Core.Command.SceCommands
{
    [SceCommand(205, "设置某个角色是否可以重叠（关闭碰撞）," +
                     "参数：角色ID，是否可重叠(1可0不可)")]
    public sealed class ActorChangeColliderSettingCommand : ICommand
    {
        public ActorChangeColliderSettingCommand(int actorId, int disableCollider)
        {
            ActorId = actorId;
            DisableCollider = disableCollider;
        }

        [SceActorId] public int ActorId { get; set; }
        public int DisableCollider { get; }
    }
}