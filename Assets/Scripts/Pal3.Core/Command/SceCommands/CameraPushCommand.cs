// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Core.Command.SceCommands
{
    #if PAL3
    [SceCommand(32, "镜头推动作，" +
                    "参数：最后from与lookAt之间的距离（原GameBox引擎下的距离单位），动作时间，插值类型（0：Linear，1：Sine）")]
    public sealed class CameraPushCommand : ICommand
    {
        public CameraPushCommand(
            float gameBoxDistance,
            float duration,
            int curveType)
        {
            GameBoxDistance = gameBoxDistance;
            Duration = duration;
            CurveType = curveType;
        }

        public float GameBoxDistance { get; }
        public float Duration { get; }
        public int CurveType { get; }
    }
    #elif PAL3A
    [SceCommand(32, "镜头推动作，" +
                    "参数：最后from与lookAt之间的距离（原GameBox引擎下的距离单位），动作时间，插值类型（0：Linear，1：Sine），同步（1暂停当前脚本运行，0异步进行动画且继续执行脚本）")]
    public sealed class CameraPushCommand : ICommand
    {
        public CameraPushCommand(
            float gameBoxDistance,
            float duration,
            int curveType,
            int synchronous)
        {
            GameBoxDistance = gameBoxDistance;
            Duration = duration;
            CurveType = curveType;
            Synchronous = synchronous;
        }

        public float GameBoxDistance { get; }
        public float Duration { get; }
        public int CurveType { get; }
        public int Synchronous { get; }
    }
    #endif
}