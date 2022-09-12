// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Command.SceCommands
{
    #if PAL3A
    [SceCommand(178, "给角色设置Y坐标" +
                     "参数：角色ID，Y坐标")]
    public class ActorSetYPositionCommand : ICommand
    {
        public ActorSetYPositionCommand(
            int actorId,
            float yPosition)
        {
            ActorId = actorId;
            YPosition = yPosition;
        }

        public int ActorId { get; }
        public float YPosition { get; }
    }
    #endif
}