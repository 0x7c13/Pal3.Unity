// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Core.Command.SceCommands
{
    [SceCommand(150, "使角色加载一个动作，" +
                    "参数：角色ID，动作编号")]
    public class ActorLoadActionCommand : ICommand
    {
        public ActorLoadActionCommand(int actorId, string actionName)
        {
            ActorId = actorId;
            ActionName = actionName;
        }

        // 角色ID为-1时 (byte值就是255) 表示当前受玩家操作的主角
        public int ActorId { get; }
        public string ActionName { get; }
    }
}