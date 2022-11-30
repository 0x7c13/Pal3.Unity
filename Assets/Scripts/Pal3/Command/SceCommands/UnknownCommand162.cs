// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Command.SceCommands
{
    #if PAL3A
    [AvailableInConsole]
    [SceCommand(162, "仙三外设置场景内的雾气特效和颜色???")]
    public class UnknownCommand162 : ICommand
    {
        public UnknownCommand162(
            float unknown1,
            float unknown2,
            float unknown3,
            float red,
            float green,
            float blue,
            float unknown7)
        {
            Unknown1 = unknown1;
            Unknown2 = unknown2;
            Unknown3 = unknown3;
            Red = red;
            Green = green;
            Blue = blue;
            Unknown7 = unknown7;
        }

        public float Unknown1 { get; }
        public float Unknown2 { get; }
        public float Unknown3 { get; }
        public float Red { get; }
        public float Green { get; }
        public float Blue { get; }
        public float Unknown7 { get; }
    }
    #endif
}