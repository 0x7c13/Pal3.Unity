// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Core.Command.SceCommands
{
    [SceCommand(205, "设置某个角色是否可以重叠（关闭碰撞）," +
                     "参数：角色ID，是否可重叠(1可0不可)")]
    public class ActorChangeColliderSettingCommand : ICommand
    {
        public ActorChangeColliderSettingCommand(int actorId, int disableCollider)
        {
            ActorId = actorId;
            DisableCollider = disableCollider;
        }

        // 角色ID为-1时 (byte值就是255) 表示当前受玩家操作的主角
        public int ActorId { get; }
        public int DisableCollider { get; }
    }
}