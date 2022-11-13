// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Command.SceCommands
{
    [AvailableInConsole]
    [SceCommand(23, "瞬间使角色面向的方向改变，" +
                    "参数：角色ID，方向（方向值为0-7）")]
    public class ActorSetFacingDirectionCommand : ICommand
    {
        public ActorSetFacingDirectionCommand(int actorId, int direction)
        {
            ActorId = actorId;
            Direction = direction;
        }

        // 角色ID为-1时 (byte值就是255) 表示当前受玩家操作的主角
        public int ActorId { get; }

        public int Direction { get; }
    }
}