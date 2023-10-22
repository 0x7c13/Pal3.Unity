// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Core.Command.SceCommands
{
    #if PAL3A
    [SceCommand(181, "???")]
    public sealed class UnknownCommand181 : ICommand
    {
        public UnknownCommand181(
            float unknown1,
            float unknown2,
            float unknown3,
            float unknown4)
        {
            Unknown1 = unknown1;
            Unknown2 = unknown2;
            Unknown3 = unknown3;
            Unknown4 = unknown4;
        }

        public float Unknown1 { get; }
        public float Unknown2 { get; }
        public float Unknown3 { get; }
        public float Unknown4 { get; }
    }
    #endif
}