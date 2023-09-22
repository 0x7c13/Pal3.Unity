// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Core.Command.SceCommands
{
    [SceCommand(30, "设置摄像机锁定角色，" +
                    "参数：角色ID")]
    public class CameraFocusOnActorCommand : ICommand
    {
        public CameraFocusOnActorCommand(int actorId)
        {
            ActorId = actorId;
        }

        public int ActorId { get; }
    }
}