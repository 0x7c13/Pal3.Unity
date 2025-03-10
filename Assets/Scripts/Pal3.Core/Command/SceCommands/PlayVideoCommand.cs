// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2025, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Core.Command.SceCommands
{
    [SceCommand(115, "播放动画")]
    public sealed class PlayVideoCommand : ICommand
    {
        public PlayVideoCommand(string name)
        {
            Name = name;
        }

        public string Name { get; }
    }
}