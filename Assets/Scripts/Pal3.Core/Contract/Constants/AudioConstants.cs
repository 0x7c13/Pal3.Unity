﻿// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2024, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Core.Contract.Constants
{
    public static class AudioConstants
    {
        public const string PlayerActorMovementSfxAudioSourceName = "PlayerActorMovementSfx";

        public const string StopMusicName = "NONE";

        #if PAL3
        public const string ThemeMusicName = "PI01";
        #elif PAL3A
        public const string ThemeMusicName = "P01";
        #endif

        #if PAL3
        public const string MainActorWalkSfxNamePrefix = "we021";
        public const string MainActorRunSfxNamePrefix = "we022";
        #elif PAL3A
        public const string MainActorWalkSfxName = "WE007";
        public const string MainActorRunSfxName= "WE008";
        public const string WangPengXuFlySfxName = "WE045";
        #endif
    }
}