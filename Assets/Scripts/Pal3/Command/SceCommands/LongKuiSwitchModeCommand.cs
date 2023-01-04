// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Command.SceCommands
{
    #if PAL3
    [AvailableInConsole]
    [SceCommand(42, "设置龙葵形象，" +
                    "参数：0蓝，1红")]
    public class LongKuiSwitchModeCommand : ICommand
    {
        public LongKuiSwitchModeCommand(int mode)
        {
            Mode = mode;
        }

        public int Mode { get; }
    }
    #endif
}