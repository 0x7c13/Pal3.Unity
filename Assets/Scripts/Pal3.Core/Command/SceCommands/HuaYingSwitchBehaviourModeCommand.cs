// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2024, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Core.Command.SceCommands
{
    [SceCommand(88, "设置花盈的行为模式," +
                    "参数：0：隐藏，1：跟随雪见，2：单飞")]
    public sealed class HuaYingSwitchBehaviourModeCommand : ICommand
    {
        public HuaYingSwitchBehaviourModeCommand(int mode)
        {
            Mode = mode;
        }

        public int Mode { get; }
    }
}