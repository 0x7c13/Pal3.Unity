// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Command.SceCommands
{
    [AvailableInConsole]
    [SceCommand(71, "使角色结束当前行为，并站立在当前位置，" +
                     "参数：角色ID")]
    public class ActorStopActionAndStandCommand : ICommand
    {
        public ActorStopActionAndStandCommand(int actorId)
        {
            ActorId = actorId;
        }

        // 角色ID为-1时 (byte值就是255) 表示当前受玩家操作的主角
        public int ActorId { get; }
    }
}