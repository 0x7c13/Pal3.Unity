// ------------------------------------------------------------------------
//  Copyright (c) 2021-2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Core.Contract.Constants
{
    using System.Collections.Generic;
    using DataReader.Cpk;
    using Enums;

    public static class EffectConstants
    {
        public const int AnimatedFireEffectFrameCount = 16;

        public const int AnimatedFireEffectFrameRate = 10;

        private const char DIR_SEPARATOR = CpkConstants.DirectorySeparatorChar;

        #if PAL3
        public static readonly Dictionary<FireEffectType, (string TexturePathFormat, string ModelPath, float Size, float lightSourceYOffset)>
            FireEffectInfo = new()
            {
                {FireEffectType.Type1, new (
                    FileConstants.EffectScnFolderVirtualPath + "Fire1" + DIR_SEPARATOR + "torch{0:00}.tga",
                    "", 3.3f, 0.5f) },
                {FireEffectType.Type2, new (
                    FileConstants.EffectScnFolderVirtualPath + "Fire2" + DIR_SEPARATOR + "{0:00}.tga",
                    "", 1f, 0.2f) },
                {FireEffectType.Type3, new (
                    FileConstants.EffectScnFolderVirtualPath + "Candle" + DIR_SEPARATOR + "{0:000}.tga",
                    $"{FileConstants.EffectScnFolderVirtualPath}Candle{DIR_SEPARATOR}candle.pol", 1f, 0.2f) },
                {FireEffectType.Type4, new (
                    FileConstants.EffectScnFolderVirtualPath + "Fire4" + DIR_SEPARATOR + "{0:00}.tga",
                    "", 5.8f, 0.5f) },
                {FireEffectType.Type5, new (
                    FileConstants.EffectScnFolderVirtualPath + "Candle" + DIR_SEPARATOR + "{0:000}.tga",
                    $"{FileConstants.EffectScnFolderVirtualPath}Lamp{DIR_SEPARATOR}lamp.pol", 0.7f, 0.2f) },
            };
        #elif PAL3A
        public static readonly Dictionary<FireEffectType, (string TexturePathFormat, string ModelPath, float Size, float lightSourceYOffset)>
            FireEffectInfo = new()
            {
                {FireEffectType.Type1, new (
                    FileConstants.EffectScnFolderVirtualPath + "Fire4" + DIR_SEPARATOR + "{0:00}.tga",
                    "", 5.8f, 0.5f) },
                {FireEffectType.Type2, new (
                    FileConstants.EffectScnFolderVirtualPath + "Fire1" + DIR_SEPARATOR + "torch{0:00}.tga",
                    "", 3.3f, 0.5f) },
                {FireEffectType.Type3, new (
                    FileConstants.EffectScnFolderVirtualPath + "Candle" + DIR_SEPARATOR + "{0:000}.tga",
                    $"{FileConstants.EffectScnFolderVirtualPath}Lamp{DIR_SEPARATOR}lamp.pol", 0.7f, 0.2f) },
                {FireEffectType.Type4, new (
                    FileConstants.EffectScnFolderVirtualPath + "Candle" + DIR_SEPARATOR + "{0:000}.tga",
                    $"{FileConstants.EffectScnFolderVirtualPath}Candle{DIR_SEPARATOR}candle.pol", 1f, 0.2f) },
                {FireEffectType.Type5, new (
                    FileConstants.EffectScnFolderVirtualPath + "Fire2" + DIR_SEPARATOR + "{0:00}.tga",
                    "", 1f, 0.3f) },
                {FireEffectType.Type6, new (
                    FileConstants.EffectScnFolderVirtualPath + "Candle" + DIR_SEPARATOR + "{0:000}.tga",
                    "", 1f, 0.6f) },
            };
        #endif
    }
}