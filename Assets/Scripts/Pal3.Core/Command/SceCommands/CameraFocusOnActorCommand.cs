// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2024, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Core.Command.SceCommands
{
    [SceCommand(30, "设置摄像机锁定角色，" +
                    "参数：角色ID")]
    public sealed class CameraFocusOnActorCommand : ICommand
    {
        public CameraFocusOnActorCommand(int actorId)
        {
            ActorId = actorId;
        }

        [SceActorId] public int ActorId { get; set; }
    }
}