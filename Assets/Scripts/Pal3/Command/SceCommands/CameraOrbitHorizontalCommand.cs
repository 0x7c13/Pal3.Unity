// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Command.SceCommands
{
    #if PAL3A
    [SceCommand(174, "镜头朝向固定点左右Orbit" +
                        "参数：目标Yaw，Pitch，目标镜头距离，运动时间，插值类型（0：Linear，1：Sine），同步（1暂停当前脚本运行，0异步进行动画且继续执行脚本）")]
    
    public class CameraOrbitHorizontalCommand : ICommand
    {
        public CameraOrbitHorizontalCommand(float yaw, float pitch, float distance, float duration, int curveType, int synchronous)
        {
            Yaw = yaw;
            Pitch = pitch;
            Distance = distance;
            Duration = duration;
            CurveType= curveType;
            Synchronous = synchronous;
        }

        public float Yaw { get; }
        public float Pitch { get; }
        public float Distance { get; }
        public float Duration { get; }
        public int CurveType { get; }
        public int Synchronous { get; }
    }
    #endif
}