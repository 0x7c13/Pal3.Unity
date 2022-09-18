// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Command.SceCommands
{
    #if PAL3
    [SceCommand(42, "设置龙葵形象，" +
                    "参数：0人，1鬼")]
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