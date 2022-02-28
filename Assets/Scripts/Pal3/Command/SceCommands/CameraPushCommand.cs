// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Command.SceCommands
{
    #if PAL3
    [SceCommand(32, "镜头推动作，" +
                    "参数：最后from与lookAt之间的距离，动作时间，插值类型（0：Linear，1：Sine）")]
    public class CameraPushCommand : ICommand
    {
        public CameraPushCommand(float distance, float duration, int curveType)
        {
            Distance = distance;
            Duration = duration;
            CurveType = curveType;
        }

        public float Distance { get; }
        public float Duration { get; }
        public int CurveType { get; }
    }
    #elif PAL3A
    [SceCommand(32, "镜头推动作，" +
                    "参数：最后from与lookAt之间的距离，动作时间，插值类型（0：Linear，1：Sine）")]
    public class CameraPushCommand : ICommand
    {
        public CameraPushCommand(float distance, float duration, int curveType, int direction)
        {
            Distance = distance;
            Duration = duration;
            CurveType = curveType;
            Direction = direction;
        }

        public float Distance { get; }
        public float Duration { get; }
        public int CurveType { get; }
        public int Direction { get; }
    }
    #endif
}