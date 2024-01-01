// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2024, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Core.Command.SceCommands
{
    #if PAL3A
    [SceCommand(216, "???")]
    public sealed class UnknownCommand216 : ICommand
    {
        public UnknownCommand216() { }
    }
    #endif
}