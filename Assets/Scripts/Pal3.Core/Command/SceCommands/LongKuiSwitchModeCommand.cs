// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2024, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Core.Command.SceCommands
{
    #if PAL3
    [SceCommand(42, "设置龙葵形象，" +
                    "参数：0蓝，1红")]
    public sealed class LongKuiSwitchModeCommand : ICommand
    {
        public LongKuiSwitchModeCommand(int mode)
        {
            Mode = mode;
        }

        public int Mode { get; }
    }
    #endif
}