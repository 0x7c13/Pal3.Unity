﻿// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Command.SceCommands
{
    #if PAL3A
    [AvailableInConsole]
    [SceCommand(189, "相机移动至默认焦距点（主角）")]
    public class CameraMoveToDefaultLookAtPointCommand : ICommand
    {
        public CameraMoveToDefaultLookAtPointCommand(int duration)
        {
            Duration = duration;
        }

        public int Duration { get; }
    }
    #endif
}