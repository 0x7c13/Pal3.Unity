// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2024, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Core.Command.SceCommands
{
    [SceCommand(110, "打开或关闭降妖谱功能，" +
                     "参数：0关闭，1打开")]
    public sealed class FeatureEnableJusticeBookCommand : ICommand
    {
        public FeatureEnableJusticeBookCommand(int enable)
        {
            Enable = enable;
        }

        public int Enable { get; }
    }
}