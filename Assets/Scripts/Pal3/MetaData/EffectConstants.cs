// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
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
        
        private static readonly char PathSeparator = CpkConstants.DirectorySeparator;

        private static readonly string EffectScnRelativePath =
            $"{FileConstants.BaseDataCpkPathInfo.cpkName}{PathSeparator}" +
            $"{FileConstants.EffectScnFolderName}{PathSeparator}";

        #if PAL3
        public static readonly Dictionary<FireEffectType, (string TexturePathFormat, string ModelPath, float Size, float lightSourceYOffset)>
            FireEffectInfo = new()
            {
                {FireEffectType.Type1, new (
                    EffectScnRelativePath + "Fire1" + PathSeparator + "torch{0:00}.tga",
                    "", 3.3f, 0.5f) },
                {FireEffectType.Type2, new (
                    EffectScnRelativePath + "Fire2" + PathSeparator + "{0:00}.tga",
                    "", 1f, 0.2f) },
                {FireEffectType.Type3, new (
                    EffectScnRelativePath + "Candle" + PathSeparator + "{0:000}.tga",
                    $"{EffectScnRelativePath}Candle{PathSeparator}candle.pol", 1f, 0.2f) },
                {FireEffectType.Type4, new (
                    EffectScnRelativePath + "Fire4" + PathSeparator + "{0:00}.tga",
                    "", 5.8f, 0.5f) },
                {FireEffectType.Type5, new (
                    EffectScnRelativePath + "Candle" + PathSeparator + "{0:000}.tga",
                    $"{EffectScnRelativePath}Lamp{PathSeparator}lamp.pol", 0.7f, 0.2f) },
            };
        #elif PAL3A
        public static readonly Dictionary<FireEffectType, (string TexturePathFormat, string ModelPath, float Size, float lightSourceYOffset)>
            FireEffectInfo = new()
            {
                {FireEffectType.Type1, new (
                    EffectScnRelativePath + "Fire4" + PathSeparator + "{0:00}.tga",
                    "", 5.8f, 0.5f) },
                {FireEffectType.Type2, new (
                    EffectScnRelativePath + "Fire1" + PathSeparator + "torch{0:00}.tga",
                    "", 3.3f, 0.5f) },
                {FireEffectType.Type3, new (
                    EffectScnRelativePath + "Candle" + PathSeparator + "{0:000}.tga",
                    $"{EffectScnRelativePath}Lamp{PathSeparator}lamp.pol", 0.7f, 0.2f) },
                {FireEffectType.Type4, new (
                    EffectScnRelativePath + "Candle" + PathSeparator + "{0:000}.tga",
                    $"{EffectScnRelativePath}Candle{PathSeparator}candle.pol", 1f, 0.2f) },
                {FireEffectType.Type5, new (
                    EffectScnRelativePath + "Fire2" + PathSeparator + "{0:00}.tga",
                    "", 1f, 0.3f) },
                {FireEffectType.Type6, new (
                    EffectScnRelativePath + "Candle" + PathSeparator + "{0:000}.tga",
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
            { 174, "WD308" },
            { 175, "WD051" },
            { 176, "WD051" },
            { 178, "WD032" },
            { 179, "WD497" },
            { 181, "WD018" },
            { 182, "WD018" },
            { 257, "WD008" },
            { 281, "WD159" },
            { 285, "WD422" },
            { 328, "WD532" },
            { 388, "WD165" },
            { 389, "WD165" },
        };
        #endif
    }
}