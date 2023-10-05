// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Core.Command.SceCommands
{
    [SceCommand(31, "设置摄像机视角锁定一个定点，" +
                    "参数：原GameBox引擎下的一个三维坐标（X，Y，Z）")]
    public class CameraFocusOnPositionCommand : ICommand
    {
        public CameraFocusOnPositionCommand(
            float gameBoxXPosition,
            float gameBoxYPosition,
            float gameBoxZPosition)
        {
            GameBoxXPosition = gameBoxXPosition;
            GameBoxYPosition = gameBoxYPosition;
            GameBoxZPosition = gameBoxZPosition;
        }

        public float GameBoxXPosition { get; }
        public float GameBoxYPosition { get; }
        public float GameBoxZPosition { get; }
    }
}