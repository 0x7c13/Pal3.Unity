// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2025, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Core.Command.SceCommands
{
    [SceCommand(22, "使角色执行一个动作，" +
                    "参数：角色ID，动作编号，播放次数（-1表示一直循环播放,-2表示播放一次后保持）")]
    public sealed class ActorPerformActionCommand : ICommand
    {
        public ActorPerformActionCommand(int actorId,
            string actionName,
            int loopCount)
        {
            ActorId = actorId;
            ActionName = actionName;
            LoopCount = loopCount;
        }

        [SceActorId] public int ActorId { get; set; }
        public string ActionName { get; }
        public int LoopCount { get; }
    }
}