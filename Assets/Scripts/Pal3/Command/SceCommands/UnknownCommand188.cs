// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Command.SceCommands
{
    #if PAL3A
    [AvailableInConsole]
    [SceCommand(188, "???")]
    public class UnknownCommand188 : ICommand
    {
        public UnknownCommand188(float unknown1,
            float unknown2,
            float unknown3,
            int unknown4)
        {
            Unknown1 = unknown1;
            Unknown2 = unknown2;
            Unknown3 = unknown3;
            Unknown4 = unknown4;
        }

        public float Unknown1 { get; }
        public float Unknown2 { get; }
        public float Unknown3 { get; }
        public int Unknown4 { get; }
    }
    #endif
}