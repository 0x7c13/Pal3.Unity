// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2024, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Core.Command.SceCommands
{
    #if PAL3A
    [SceCommand(176, "南宫煌开启摄灵法阵")]
    public sealed class UnknownCommand176 : ICommand
    {
        public UnknownCommand176(
            int unknown1,
            int unknown2,
            int unknown3)
        {
            Unknown1 = unknown1;
            Unknown2 = unknown2;
            Unknown3 = unknown3;
        }

        public int Unknown1 { get; }
        public int Unknown2 { get; }
        public int Unknown3 { get; }
    }
    #endif
}