// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE.txt in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Command.SceCommands
{
    [SceCommand(24, "转动角色面向的方向，" +
                    "参数：角色ID，转过多少角度（-360~360）")]
    public class ActorRotateFacingCommand : ICommand
    {
        public ActorRotateFacingCommand(int actorId, int degrees)
        {
            ActorId = actorId;
            Degrees = degrees;
        }

        // 角色ID为-1时 (byte值就是255) 表示当前受玩家操作的主角
        public int ActorId { get; }

        public int Degrees { get; }
    }
}