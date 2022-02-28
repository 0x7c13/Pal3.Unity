// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE.txt in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Command.SceCommands
{
    [SceCommand(88, "设置花盈的行为模式," +
                    "参数：0：隐藏，1：跟随雪见，2：单飞")]
    public class HuaYingSwitchBehaviourModeCommand : ICommand
    {
        public HuaYingSwitchBehaviourModeCommand(int mode)
        {
            Mode = mode;
        }

        public int Mode { get; }
    }
}