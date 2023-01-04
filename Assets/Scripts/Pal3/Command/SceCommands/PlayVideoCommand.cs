// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Command.SceCommands
{
    [AvailableInConsole]
    [SceCommand(115, "播放动画")]
    public class PlayVideoCommand : ICommand
    {
        public PlayVideoCommand(string name)
        {
            Name = name;
        }

        public string Name { get; }
    }
}