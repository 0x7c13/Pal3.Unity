// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Command.SceCommands
{
    #if PAL3A
    [SceCommand(177, "王蓬絮开启朱仙变???")]
    public class UnknownCommand177: ICommand
    {
        public UnknownCommand177(
            int enable)
        {
            Enable = enable;
        }

        public int Enable { get; }
    }
    #endif
}