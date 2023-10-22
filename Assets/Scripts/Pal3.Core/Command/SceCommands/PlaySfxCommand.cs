// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Core.Command.SceCommands
{
    [SceCommand(79, "播放音效，" +
                    "参数：音效名，循环次数（0表示开始无限循环，-1表示结束循环）")]
    public sealed class PlaySfxCommand : ICommand
    {
        public PlaySfxCommand(string sfxName, int loopCount)
        {
            SfxName = sfxName;
            LoopCount = loopCount;
        }

        public string SfxName { get; }
        public int LoopCount { get; }
    }
}