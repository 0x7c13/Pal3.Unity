// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Command.SceCommands
{
    #if PAL3A
    [AvailableInConsole]
    [SceCommand(182, "???")]
    public class UnknownCommand182 : ICommand
    {
        public UnknownCommand182(
            string unknown)
        {
            Unknown = unknown;
        }

        public string Unknown { get; }
    }
    #endif
}