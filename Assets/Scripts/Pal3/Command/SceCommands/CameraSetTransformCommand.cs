// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Command.SceCommands
{
    [SceCommand(36, "设置镜头参数，" +
                    "参数：Yaw，Pitch，Distance，X，Y，Z")]
    public class CameraSetTransformCommand : ICommand
    {
        public CameraSetTransformCommand(float yaw, float pitch, float distance, float x, float y, float z)
        {
            Yaw = yaw;
            Pitch = pitch;
            Distance = distance;
            X = x;
            Y = y;
            Z = z;
        }

        public float Yaw { get; }
        public float Pitch { get; }
        public float Distance { get; }
        public float X { get; }
        public float Y { get; }
        public float Z { get; }
    }
}