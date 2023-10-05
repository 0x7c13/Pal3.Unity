// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Core.Command.SceCommands
{
    [SceCommand(33, "镜头环绕LookAt位置旋转（LookAt点固定），" +
                    "参数：目标Yaw，Pitch，运动时间，插值类型（0：Linear，1：Sine）")]
    public class CameraOrbitCommand : ICommand
    {
        public CameraOrbitCommand(
            float yaw,
            float pitch,
            float duration,
            int curveType)
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