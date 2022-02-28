// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Command.SceCommands
{
    [SceCommand(35, "镜头摇动（from点固定），" +
                    "参数：目标Yaw，Pitch，运动时间，插值类型（0：Linear，1：Sine）")]
    public class CameraRotateCommand : ICommand
    {
        public CameraRotateCommand(float yaw, float pitch, float duration, int curveType)
        {
            Yaw = yaw;
            Pitch = pitch;
            Duration = duration;
            CurveType = curveType;
        }

        public float Yaw { get; }
        public float Pitch { get; }
        public float Duration { get; }
        public int CurveType { get; }
    }
}