// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE.txt in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Command.SceCommands
{
    [SceCommand(210, "转动角色的方向，" +
                     "参数：ID,方向（方向值为0-7）")]
    public class ActorRotateFacingDirectionCommand : ICommand
    {
        public ActorRotateFacingDirectionCommand(int actorId, int direction)
        {
            ActorId = actorId;
            Direction = direction;
        }

        // 角色ID为-1时 (byte值就是255) 表示当前受玩家操作的主角
        public int ActorId { get; }

        public int Direction { get; }
    }
}