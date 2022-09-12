// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Command.SceCommands
{
    #if PAL3A
    [SceCommand(167, "雷电特效???")]
    public class UnknownCommand167 : ICommand
    {
        public UnknownCommand167(
            string unknown1,
            int unknown2,
            float unknown3,
            float unknown4,
            float unknown5,
            float unknown6,
            float unknown7,
            float unknown8)
        {
            Unknown1 = unknown1;
            Unknown2 = unknown2;
            Unknown3 = unknown3;
            Unknown4 = unknown4;
            Unknown5 = unknown5;
            Unknown6 = unknown6;
            Unknown7 = unknown7;
            Unknown8 = unknown8;
        }
        
        public string Unknown1 { get; }
        public int Unknown2 { get; }
        public float Unknown3 { get; }
        public float Unknown4 { get; }
        public float Unknown5 { get; }
        public float Unknown6 { get; }
        public float Unknown7 { get; }
        public float Unknown8 { get; }
    }
    #endif
}