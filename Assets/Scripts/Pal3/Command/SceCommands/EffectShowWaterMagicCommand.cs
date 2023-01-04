// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Command.SceCommands
{
    [AvailableInConsole]
    [SceCommand(152, "在玩家角色周身渲染水特效，" +
                     "参数：0关闭，1开启")]
    public class EffectShowWaterMagicCommand : ICommand
    {
        public EffectShowWaterMagicCommand(int active)
        {
            Active = active;
        }

        public int Active { get; }
    }
}