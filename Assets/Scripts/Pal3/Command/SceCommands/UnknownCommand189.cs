// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Command.SceCommands
{
    #if PAL3A
    [AvailableInConsole]
    [SceCommand(189, "???")]
    public class UnknownCommand189 : ICommand
    {
        public UnknownCommand189(int unknown)
        {
            Unknown = unknown;
        }

        public int Unknown { get; }
    }
    #endif
}