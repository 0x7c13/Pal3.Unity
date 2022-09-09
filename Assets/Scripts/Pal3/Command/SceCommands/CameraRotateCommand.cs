// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Command.SceCommands
{
    #if PAL3
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
    #elif PAL3A
    [SceCommand(35, "镜头摇动（from点固定），" +
                    "参数：目标Yaw，Pitch，运动时间，插值类型（0：Linear，1：Sine），同步（1暂停当前脚本运行，0异步进行动画且继续执行脚本）")]
    public class CameraRotateCommand : ICommand
    {
        public CameraRotateCommand(float yaw, float pitch, float duration, int curveType, int synchronous)
        {
            Yaw = yaw;
            Pitch = pitch;
            Duration = duration;
            CurveType = curveType;
            Synchronous = synchronous;
        }

        public float Yaw { get; }
        public float Pitch { get; }
        public float Duration { get; }
        public int CurveType { get; }
        public int Synchronous { get; }
    }
    #endif
}