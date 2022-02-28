// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE.txt in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Command.SceCommands
{
    [SceCommand(109, "打开或关闭魔剑养成功能，" +
                     "参数：0关闭1打开")]
    public class FeatureEnableSwordPurifyingCommand : ICommand
    {
        public FeatureEnableSwordPurifyingCommand(int enable)
        {
            Enable = enable;
        }

        public int Enable { get; }
    }
}