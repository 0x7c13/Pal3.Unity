// ------------------------------------------------------------------------
//  Copyright (c) 2021-2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

#if PAL3A
namespace Pal3.Core.Command.SceCommands
{
    [SceCommand(176, "开启或关闭南宫煌特殊战斗能力")]
    public sealed class NanGongHuangEnableCombatAbilitiesCommand : ICommand
    {
        public NanGongHuangEnableCombatAbilitiesCommand(int enable)
        {
            Enable = enable;
        }

        public int Enable { get; }
    }
}
#endif
