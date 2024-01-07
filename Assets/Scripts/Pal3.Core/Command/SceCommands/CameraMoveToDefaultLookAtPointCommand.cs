﻿// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2024, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Core.Command.SceCommands
{
    #if PAL3A
    [SceCommand(189, "相机移动至默认焦距点（主角）")]
    public sealed class CameraMoveToDefaultLookAtPointCommand : ICommand
    {
        public CameraMoveToDefaultLookAtPointCommand(int duration)
        {
            Duration = duration;
        }

        public int Duration { get; }
    }
    #endif
}