﻿// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2024, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Core.Command.SceCommands
{
    #if PAL3A
    [SceCommand(188, "移动相机并看向固定点")]
    public sealed class CameraMoveToLookAtPointCommand : ICommand
    {
        public CameraMoveToLookAtPointCommand(
            float gameBoxXPosition,
            float gameBoxYPosition,
            float gameBoxZPosition,
            int synchronous)
        {
            GameBoxXPosition = gameBoxXPosition;
            GameBoxYPosition = gameBoxYPosition;
            GameBoxZPosition = gameBoxZPosition;
            Synchronous = synchronous;
        }

        public float GameBoxXPosition { get; }
        public float GameBoxYPosition { get; }
        public float GameBoxZPosition { get; }
        public int Synchronous { get; }
    }
    #endif
}