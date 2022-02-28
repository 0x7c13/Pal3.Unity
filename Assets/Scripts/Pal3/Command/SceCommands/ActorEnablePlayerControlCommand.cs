// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE.txt in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Command.SceCommands
{
    [SceCommand(204, "设置玩家控制哪个主角，" +
                    "参数：主角ID")]
    public class ActorEnablePlayerControlCommand : ICommand
    {
        public ActorEnablePlayerControlCommand(int actorId)
        {
            ActorId = actorId;
        }

        // 角色ID为-1时 (byte值就是255) 表示当前受玩家操作的主角
        public int ActorId { get; }
    }
}