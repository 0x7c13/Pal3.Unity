// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Command.SceCommands
{
    [SceCommand(133, "播放剧情音乐")]
    public class PlayMusicCommand : ICommand
    {
        public PlayMusicCommand(string musicName, int loop)
        {
            MusicName = musicName;
            Loop = loop;
        }

        // Music名为"NONE"的时候代表停止播放当前音乐
        public string MusicName { get; }
        public int Loop { get; }
    }
}