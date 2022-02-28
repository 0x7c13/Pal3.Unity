// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE.txt in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Command.SceCommands
{
    #if PAL3A
    [SceCommand(170, "???")]
    public class UnknownCommand170 : ICommand
    {
        public UnknownCommand170(string unknown)
        {
            Unknown = unknown;
        }

        public string Unknown { get; }
    }
    #endif
}