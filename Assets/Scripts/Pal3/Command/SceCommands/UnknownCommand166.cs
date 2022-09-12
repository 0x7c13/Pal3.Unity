// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Command.SceCommands
{
    #if PAL3A
    [SceCommand(166, "???")]
    public class UnknownCommand166 : ICommand
    {
        public UnknownCommand166(
            int unknown1,
            int unknown2)
        {
            Unknown1 = unknown1;
            Unknown2 = unknown2;
        }
        
        public int Unknown1 { get; }
        public int Unknown2 { get; }
    }
    #endif
}