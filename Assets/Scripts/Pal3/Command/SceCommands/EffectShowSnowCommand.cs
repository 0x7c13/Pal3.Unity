// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Command.SceCommands
{
    [SceCommand(140, "开启下雪特效，" +
                     "参数：0关闭，1开启")]
    public class EffectShowSnowCommand : ICommand
    {
        public EffectShowSnowCommand(int active)
        {
            Active = active;
        }

        public int Active { get; }
    }
}