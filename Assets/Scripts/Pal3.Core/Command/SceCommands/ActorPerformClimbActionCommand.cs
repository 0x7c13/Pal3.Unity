// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Core.Command.SceCommands
{
    [SceCommand(74, "使当前玩家操纵角色攀爬场景物品，" +
                    "参数：场景物品ID，向上或向下（1上0下）")]
    public sealed class ActorPerformClimbActionCommand : ICommand
    {
        public ActorPerformClimbActionCommand(int objectId, int climbUp)
        {
            ObjectId = objectId;
            ClimbUp = climbUp;
        }

        public int ObjectId { get; }
        public int ClimbUp { get; }
    }
}