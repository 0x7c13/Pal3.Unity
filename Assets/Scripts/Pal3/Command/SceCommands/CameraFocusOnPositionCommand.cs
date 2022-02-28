// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Command.SceCommands
{
    [SceCommand(31, "设置摄像机锁定一个定点，" +
                    "参数：LookAtX,LookAtY,LookAtZ")]
    public class CameraFocusOnPositionCommand : ICommand
    {
        public CameraFocusOnPositionCommand(float lookAtX, float lookAtY, float lookAtZ)
        {
            LookAtX = lookAtX;
            LookAtY = lookAtY;
            LookAtZ = lookAtZ;
        }

        public float LookAtX { get; }
        public float LookAtY { get; }
        public float LookAtZ { get; }
    }
}