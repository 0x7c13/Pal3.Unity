// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

#if PAL3A

namespace Pal3.Game.Dev
{
    using System;
    using System.Collections.Generic;

    public static class TexturePatcher
    {
        // These textures have wrong scaling/tiling in PAL3A game,
        // Scale should be (1, 1) by default but these are (1, -1) in PAL3A
        private static readonly HashSet<string> KnownUpsideDownTextureFilesInPal3A = new (StringComparer.OrdinalIgnoreCase)
        {
            "basedata.cpk\\role\\607\\607.tga",
            "basedata.cpk\\role\\611\\611.tga",
            "basedata.cpk\\role\\613\\613.tga",
            "basedata.cpk\\role\\b03\\b03.tga",
            "basedata.cpk\\role\\b04\\b04.tga",
            "basedata.cpk\\role\\b07\\b07.tga",
            "basedata.cpk\\role\\b08\\b08.tga",
            "basedata.cpk\\role\\b10\\B10.tga",
            "basedata.cpk\\role\\b11\\B11.tga",
            "basedata.cpk\\role\\b12\\b12.tga",
            "basedata.cpk\\role\\b19\\b19.tga",
            "basedata.cpk\\role\\b22\\b22.tga",
        };

        public static bool TextureFileHasWrongTiling(string textureFile)
        {
            return KnownUpsideDownTextureFilesInPal3A.Contains(textureFile);
        }
    }
}

#endif