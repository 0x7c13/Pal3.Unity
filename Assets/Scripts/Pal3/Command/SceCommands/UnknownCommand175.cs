// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Command.SceCommands
{
    #if PAL3A
    [SceCommand(175, "镜头朝向固定点上下Orbit?")]
    public class UnknownCommand175 : ICommand
    {
        public UnknownCommand175(float x, float y, float z, float duration, int mode, int direction)
        {
            X = x;
            Y = y;
            Z = z;
            Duration = duration;
            Mode= mode;
            Direction = direction;
        }

        public float X { get; }
        public float Y { get; }
        public float Z { get; }
        public float Duration { get; }
        public int Mode { get; }
        public int Direction { get; }
    }
    #endif
}