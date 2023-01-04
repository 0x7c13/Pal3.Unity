// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Command.SceCommands
{
    [AvailableInConsole]
    [SceCommand(207, "指定角色是否在PerformAction指令完成后自动切换成站立的动作，" +
                     "参数：角色ID，是否自动站立（1是0否）")]
    public class ActorAutoStandCommand : ICommand
    {
        public ActorAutoStandCommand(int actorId, int autoStand)
        {
            ActorId = actorId;
            AutoStand = autoStand;
        }

        // 角色ID为-1时 (byte值就是255) 表示当前受玩家操作的主角
        public int ActorId { get; }
        public int AutoStand { get; }
    }
}