// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2025, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Core.Command.SceCommands
{
    [SceCommand(150, "使角色预加载一个动作，" +
                    "参数：角色ID，动作编号")]
    public sealed class ActorLoadActionCommand : ICommand
    {
        public ActorLoadActionCommand(int actorId, string actionName)
        {
            ActorId = actorId;
            ActionName = actionName;
        }

        [SceActorId] public int ActorId { get; set; }
        public string ActionName { get; }
    }
}