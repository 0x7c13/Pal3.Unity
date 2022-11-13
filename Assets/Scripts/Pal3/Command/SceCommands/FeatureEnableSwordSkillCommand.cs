// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Command.SceCommands
{
    [AvailableInConsole]
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
}