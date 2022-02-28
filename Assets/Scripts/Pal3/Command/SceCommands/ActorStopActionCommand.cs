// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Command.SceCommands
{
    [SceCommand(221, "ActorPerformActionCommand的循环次数为-1或-2时需要用此命令结束，" +
                    "参数：角色ID")]
    public class ActorStopActionCommand : ICommand
    {
        public ActorStopActionCommand(int actorId)
        {
            ActorId = actorId;
        }

        // 角色ID为-1时 (byte值就是255) 表示当前受玩家操作的主角
        public int ActorId { get; }
    }
}