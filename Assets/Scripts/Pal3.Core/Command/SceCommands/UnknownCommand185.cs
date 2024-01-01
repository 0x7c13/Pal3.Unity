// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2024, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Core.Command.SceCommands
{
    #if PAL3A
    [SceCommand(185, "PAL3A打开算命小游戏UI")]
    public sealed class UnknownCommand185 : ICommand
    {
        public UnknownCommand185(int unknown1,
            int unknown2,
            int unknown3,
            int unknown4,
            int unknown5,
            int unknown6,
            int unknown7,
            int unknown8)
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

        public int Unknown1 { get; }
        public int Unknown2 { get; }
        public int Unknown3 { get; }
        public int Unknown4 { get; }
        public int Unknown5 { get; }
        public int Unknown6 { get; }
        public int Unknown7 { get; }
        public int Unknown8 { get; }
    }
#endif
}