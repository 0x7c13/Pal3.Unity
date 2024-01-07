// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2024, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Core.Command.SceCommands
{
    [SceCommand(155, "相机设置Yaw角度")]
    public sealed class CameraSetYawCommand : ICommand
    {
        public CameraSetYawCommand(float yaw)
        {
            Yaw = yaw;
        }

        public float Yaw { get; }
    }
}