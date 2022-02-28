// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE.txt in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Command.SceCommands
{
    [SceCommand(79, "播放音效，" +
                    "参数：音效名，循环次数")]
    public class PlaySfxCommand : ICommand
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