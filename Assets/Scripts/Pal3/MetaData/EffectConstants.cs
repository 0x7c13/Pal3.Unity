// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.MetaData
{
    using System.Collections.Generic;
    using Core.DataReader.Cpk;

    public enum GraphicsEffect
    {
        None        = -1,
        PoisonFog   =  0,  // 雪见.瘴气
        Portal      =  1,  // 传送点
        SavePoint   =  2,  // 存盘点
        Fire        =  3,  // 火焰类特效
        Combat      =  4,  // 战斗特效
    }

    public enum FireEffectType
    {
        Type1 = 0,
        Type2,
        Type3,
        Type4,
        Type5,
        #if PAL3A
        Type6,
        #endif
    }

    public static class EffectConstants
    {
        public const int AnimatedFireEffectFrameCount = 16;

        public const int AnimatedFireEffectFrameRate = 10;

        private const char DirSeparator = CpkConstants.DirectorySeparatorChar;

        #if PAL3
        public static readonly Dictionary<FireEffectType, (string TexturePathFormat, string ModelPath, float Size, float lightSourceYOffset)>
            FireEffectInfo = new()
            {
                {FireEffectType.Type1, new (
                    FileConstants.EffectScnFolderVirtualPath + "Fire1" + DirSeparator + "torch{0:00}.tga",
                    "", 3.3f, 0.5f) },
                {FireEffectType.Type2, new (
                    FileConstants.EffectScnFolderVirtualPath + "Fire2" + DirSeparator + "{0:00}.tga",
                    "", 1f, 0.2f) },
                {FireEffectType.Type3, new (
                    FileConstants.EffectScnFolderVirtualPath + "Candle" + DirSeparator + "{0:000}.tga",
                    $"{FileConstants.EffectScnFolderVirtualPath}Candle{DirSeparator}candle.pol", 1f, 0.2f) },
                {FireEffectType.Type4, new (
                    FileConstants.EffectScnFolderVirtualPath + "Fire4" + DirSeparator + "{0:00}.tga",
                    "", 5.8f, 0.5f) },
                {FireEffectType.Type5, new (
                    FileConstants.EffectScnFolderVirtualPath + "Candle" + DirSeparator + "{0:000}.tga",
                    $"{FileConstants.EffectScnFolderVirtualPath}Lamp{DirSeparator}lamp.pol", 0.7f, 0.2f) },
            };
        #elif PAL3A
        public static readonly Dictionary<FireEffectType, (string TexturePathFormat, string ModelPath, float Size, float lightSourceYOffset)>
            FireEffectInfo = new()
            {
                {FireEffectType.Type1, new (
                    FileConstants.EffectScnFolderVirtualPath + "Fire4" + DirSeparator + "{0:00}.tga",
                    "", 5.8f, 0.5f) },
                {FireEffectType.Type2, new (
                    FileConstants.EffectScnFolderVirtualPath + "Fire1" + DirSeparator + "torch{0:00}.tga",
                    "", 3.3f, 0.5f) },
                {FireEffectType.Type3, new (
                    FileConstants.EffectScnFolderVirtualPath + "Candle" + DirSeparator + "{0:000}.tga",
                    $"{FileConstants.EffectScnFolderVirtualPath}Lamp{DirSeparator}lamp.pol", 0.7f, 0.2f) },
                {FireEffectType.Type4, new (
                    FileConstants.EffectScnFolderVirtualPath + "Candle" + DirSeparator + "{0:000}.tga",
                    $"{FileConstants.EffectScnFolderVirtualPath}Candle{DirSeparator}candle.pol", 1f, 0.2f) },
                {FireEffectType.Type5, new (
                    FileConstants.EffectScnFolderVirtualPath + "Fire2" + DirSeparator + "{0:00}.tga",
                    "", 1f, 0.3f) },
                {FireEffectType.Type6, new (
                    FileConstants.EffectScnFolderVirtualPath + "Candle" + DirSeparator + "{0:000}.tga",
                    "", 1f, 0.6f) },
            };
        #endif

        #if PAL3
        // Effect group id to sfx mapping
        public static readonly Dictionary<int, string> EffectSfxInfo = new()
        {
            { 251, "wd163" },
            { 252, "wd160" },
            { 254, "wd163" },
            { 255, "wd155" },
            { 256, "wd008" },
            { 257, "wd001" },
            { 261, "wd163" },
            { 264, "wd160" },
            { 266, "wd160" },
            { 276, "wd150" },
            { 278, "wd150" },
            { 297, "wd141" },
            { 341, "wd160" },
            { 457, "wd141" },
            { 458, "wd140" },
        };
        #elif PAL3A
        public static readonly Dictionary<int, string> EffectSfxInfo = new()
        {
            { 91,  "WD156" },
            { 170, "WD155" },
            { 171, "WD163" },
            { 172, "WD189" },
            { 173, "WD189" },
            { 174, "WD308" },
            { 175, "WD051" },
            { 176, "WD051" },
            { 177, "WD159" },
            { 178, "WD032" },
            { 179, "WD497" },
            { 181, "WD018" },
            { 182, "WD018" },
            { 189, "WD035" },
            { 198, "WD018" },
            { 209, "WD309" },
            { 211, "WD002" },
            { 230, "WD342" },
            { 254, "WD163" },
            { 257, "WD008" },
            { 258, "WD165" },
            { 259, "WD163" },
            { 281, "WD159" },
            { 283, "WD018" },
            { 285, "WD422" },
            { 290, "WD418" },
            { 299, "WD150" },
            { 322, "WD163" },
            { 323, "WD189" },
            { 328, "WD532" },
            { 388, "WD165" },
            { 389, "WD165" },
            { 397, "WD459" },
            { 408, "WD459" },
        };
        #endif
    }
}