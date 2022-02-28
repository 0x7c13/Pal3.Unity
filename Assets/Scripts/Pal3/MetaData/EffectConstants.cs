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
        Fire        =  3,  // 火把
        Combat      =  4,  // 战斗特效
    }

    public enum FireEffectType
    {
        Fire1 = 0,
        Fire2,
        Fire3,
        Fire4,
        Fire5
    }

    public static class EffectConstants
    {
        private static readonly char PathSeparator = CpkConstants.CpkDirectorySeparatorChar;

        private static readonly string EffectScnRelativePath =
            $"{FileConstants.BaseDataCpkPathInfo.cpkName}{PathSeparator}" +
            $"{FileConstants.EffectScnFolderName}{PathSeparator}";

        public static readonly Dictionary<FireEffectType, (string TexturePathFormat, string ModelPath, float Size)>
            FireEffectInfo = new()
        {
            {FireEffectType.Fire1, new (
                EffectScnRelativePath + "Fire1" + PathSeparator + "torch{0:00}.tga",
                "", 3.3f) },
            {FireEffectType.Fire2, new (
                EffectScnRelativePath + "Fire2" + PathSeparator + "huo{0:00}.tga",
                "", 1f) },
            {FireEffectType.Fire3, new (
                EffectScnRelativePath + "Candle" + PathSeparator + "{0:000}.tga",
                $"{EffectScnRelativePath}Candle{PathSeparator}Candle.pol", 1f) },
            {FireEffectType.Fire4, new (
                EffectScnRelativePath + "Fire4" + PathSeparator + "{0:000}.tga",
                "", 5.8f) },
            {FireEffectType.Fire5, new (
                EffectScnRelativePath + "Candle" + PathSeparator + "{0:000}.tga",
                $"{EffectScnRelativePath}Lamp{PathSeparator}Lamp.pol", 0.7f) },
        };

        public static readonly Dictionary<GraphicsEffect, (int NumberOfFrames, int Fps)> EffectAnimationInfo = new()
        {
            {GraphicsEffect.PoisonFog, new (0, 0) },
            {GraphicsEffect.Portal, new (0, 0)},
            {GraphicsEffect.SavePoint, new (0, 0)},
            {GraphicsEffect.Fire, new (16, 10)},
            {GraphicsEffect.Combat, new (0, 0)},
        };
    }
}