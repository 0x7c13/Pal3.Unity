// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Command.SceCommands
{
    [SceCommand(28, "设置角色是否激活，" +
                    "参数：角色ID，是否激活（1是0否）")]
    public class ActorActivateCommand : ICommand
    {
        public ActorActivateCommand(int actorId, int isActive)
        {
            ActorId = actorId;
            IsActive = isActive;
        }

        // 角色ID为-1时 (byte值就是255) 表示当前受玩家操作的主角
        public int ActorId { get; }
        public int IsActive { get; }
    }
}