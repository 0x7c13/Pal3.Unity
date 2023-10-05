// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Core.Command.SceCommands
{
    #if PAL3
    [SceCommand(138, "打开或关闭魔剑技功能，" +
                     "参数：0关闭，1打开")]
    public class FeatureEnableSwordSkillCommand : ICommand
    {
        public FeatureEnableSwordSkillCommand(int enable)
        {
            Enable = enable;
        }

        public int Enable { get; }
    }
    #endif
}