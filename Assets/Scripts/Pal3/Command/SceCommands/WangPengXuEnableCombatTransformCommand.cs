// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Command.SceCommands
{
    #if PAL3A
    [AvailableInConsole]
    [SceCommand(177, "设置开启/关闭王蓬絮战斗中变身（朱仙变），" +
                     "参数：0关闭，1开启")]
    public class WangPengXuEnableCombatTransformCommand: ICommand
    {
        public WangPengXuEnableCombatTransformCommand(
            int enabled)
        {
            Enabled = enabled;
        }

        public int Enabled { get; }
    }
    #endif
}