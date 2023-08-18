// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.MetaData
{
    public static class AudioConstants
    {
        public const string PlayerActorMovementSfxAudioSourceName = "PlayerActorMovementSfx";

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