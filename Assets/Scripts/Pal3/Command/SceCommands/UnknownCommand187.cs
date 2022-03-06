// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Command.SceCommands
{
    #if PAL3A
    [SceCommand(187, "播放开门动画？参数为门的ID")]
    public class UnknownCommand187 : ICommand
    {
        public UnknownCommand187(int unknown)
        {
            Unknown = unknown;
        }

        public int Unknown { get; }
    }
    #endif
}