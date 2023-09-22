// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Command.Extensions
{
    using Core.Command;

    [AvailableInConsole]
    public class ActorSetFacingCommand : ICommand
    {
        public ActorSetFacingCommand(int actorId, int degrees)
        {
            ActorId = actorId;
            Degrees = degrees;
        }

        // 角色ID为-1时 (byte值就是255) 表示当前受玩家操作的主角
        public int ActorId { get; }
        public int Degrees { get; }
    }
}