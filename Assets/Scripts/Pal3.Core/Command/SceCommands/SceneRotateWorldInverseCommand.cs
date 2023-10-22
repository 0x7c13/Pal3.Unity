// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Core.Command.SceCommands
{
    #if PAL3
    [SceCommand(128, "反方向旋转整个场景，" +
                    "参数：原GameBox坐标系下的X轴旋转角度, Y轴旋转角度, Z轴旋转角度")]
    public sealed class SceneRotateWorldInverseCommand : ICommand
    {
        public SceneRotateWorldInverseCommand(int xDegrees, int yDegrees, int zDegrees)
        {
            XDegrees = xDegrees;
            YDegrees = yDegrees;
            ZDegrees = zDegrees;
        }

        public int XDegrees { get; }
        public int YDegrees { get; }
        public int ZDegrees { get; }
    }
    #endif
}