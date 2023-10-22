// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Core.Command.SceCommands
{
    [SceCommand(36, "设置镜头参数，" +
                    "参数：Yaw，Pitch，Distance（原GameBox引擎下的距离单位），原GameBox引擎下的一个三维坐标（X，Y，Z）")]
    public sealed class CameraSetTransformCommand : ICommand
    {
        public CameraSetTransformCommand(
            float yaw,
            float pitch,
            float gameBoxDistance,
            float gameBoxXPosition,
            float gameBoxYPosition,
            float gameBoxZPosition)
        {
            Yaw = yaw;
            Pitch = pitch;
            GameBoxDistance = gameBoxDistance;
            GameBoxXPosition = gameBoxXPosition;
            GameBoxYPosition = gameBoxYPosition;
            GameBoxZPosition = gameBoxZPosition;
        }

        public float Yaw { get; }
        public float Pitch { get; }
        public float GameBoxDistance { get; }
        public float GameBoxXPosition { get; }
        public float GameBoxYPosition { get; }
        public float GameBoxZPosition { get; }
    }
}