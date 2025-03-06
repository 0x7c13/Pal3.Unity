// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2025, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Core.Command.SceCommands
{
    #if PAL3A
    [SceCommand(177, "设置开启/关闭王蓬絮战斗中变身（朱仙变），" +
                     "参数：0关闭，1开启")]
    public sealed class WangPengXuEnableCombatTransformCommand: ICommand
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