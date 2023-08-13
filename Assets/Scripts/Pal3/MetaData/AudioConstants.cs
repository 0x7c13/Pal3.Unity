// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.MetaData
{
    using System.Collections.Generic;

    public static class AudioConstants
    {
        public const string PlayerActorMovementSfxAudioSourceName = "PlayerActorMovementSfx";

        public static readonly HashSet<string> PlayerActorMovementSfxAudioFileNames = new ()
        {
            #if PAL3
            "we021a.wav",
            "we021b.wav",
            "we021c.wav",
            "we021d.wav",
            "we022a.wav",
            "we022b.wav",
            "we022c.wav",
            "we022d.wav",
            #elif PAL3A
            "WE007.WAV",
            "WE008.WAV"
            #endif
        };

        #if PAL3
        public const string ThemeMusicName = "PI01";
        #elif PAL3A
        public const string ThemeMusicName = "P01";
        #endif
    }
}