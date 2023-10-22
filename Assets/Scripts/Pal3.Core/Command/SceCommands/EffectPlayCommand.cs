// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Core.Command.SceCommands
{
    [SceCommand(143, "播放VFX特效，" +
                     "参数：VFX特效GroupID")]
    public sealed class EffectPlayCommand : ICommand
    {
        public EffectPlayCommand(int effectGroupId)
        {
            EffectGroupId = effectGroupId;
        }

        public int EffectGroupId { get; }
    }
}