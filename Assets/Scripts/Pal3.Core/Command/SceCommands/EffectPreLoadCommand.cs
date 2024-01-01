// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2024, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Core.Command.SceCommands
{
    [SceCommand(148, "预加载VFX特效Group，" +
                     "参数：VFX特效GroupID")]
    public sealed class EffectPreLoadCommand : ICommand
    {
        public EffectPreLoadCommand(int effectGroupId)
        {
            EffectGroupId = effectGroupId;
        }

        public int EffectGroupId { get; }
    }
}