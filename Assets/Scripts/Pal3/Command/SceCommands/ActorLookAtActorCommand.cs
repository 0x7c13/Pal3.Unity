// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Command.SceCommands
{
    [AvailableInConsole]
    [SceCommand(209, "设定一个角色面向另一个角色（的位置），" +
                     "参数：被设置的角色的ID，面向的角色的ID")]
    public class ActorLookAtActorCommand : ICommand
    {
        public ActorLookAtActorCommand(int actorId, int lookAtActorId)
        {
            ActorId = actorId;
            LookAtActorId = lookAtActorId;
        }

        // 角色ID为-1时 (byte值就是255) 表示当前受玩家操作的主角
        public int ActorId { get; }

        public int LookAtActorId { get; }
    }
}