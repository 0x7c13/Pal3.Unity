// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2025, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Core.Command.SceCommands
{
    [SceCommand(152, "在玩家角色周身渲染水特效，" +
                     "参数：0关闭，1开启")]
    public sealed class EffectShowWaterMagicCommand : ICommand
    {
        public EffectShowWaterMagicCommand(int active)
        {
            Active = active;
        }

        public int Active { get; }
    }
}