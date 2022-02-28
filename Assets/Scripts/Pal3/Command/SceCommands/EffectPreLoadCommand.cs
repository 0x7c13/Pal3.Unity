// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Command.SceCommands
{
    [SceCommand(148, "预加载特效Group，" +
                     "参数：特效GroupID")]
    public class EffectPreLoadCommand : ICommand
    {
        public EffectPreLoadCommand(int effectGroupId)
        {
            EffectGroupId = effectGroupId;
        }

        public int EffectGroupId { get; }
    }
}