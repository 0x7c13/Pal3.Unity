// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Command.SceCommands
{
    [SceCommand(22, "使角色执行一个动作，" +
                    "参数：角色ID，动作编号，播放次数（-1表示一直循环播放,-2表示播放一次后保持）")]
    public class ActorPerformActionCommand : ICommand
    {
        public ActorPerformActionCommand(int actorId, string actionName, int loopCount)
        {
            ActorId = actorId;
            ActionName = actionName;
            LoopCount = loopCount;
        }

        // 角色ID为-1时 (byte值就是255) 表示当前受玩家操作的主角
        public int ActorId { get; }
        public string ActionName { get; }
        public int LoopCount { get; }
    }
}