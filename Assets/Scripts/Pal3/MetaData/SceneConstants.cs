// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.MetaData
{
    using System.Collections.Generic;
    using Core.DataReader.Cpk;

    public static class SceneConstants
    {
        private static readonly char PathSeparator = CpkConstants.DirectorySeparator;

        private static readonly string EffectScnRelativePath =
            $"{FileConstants.BaseDataCpkPathInfo.cpkName}{PathSeparator}" +
            $"{FileConstants.EffectScnFolderName}{PathSeparator}";

        public static readonly string[] SkyBoxTexturePathFormat =
        {
            EffectScnRelativePath + "skybox" + PathSeparator + "{0:00}" + PathSeparator + "{0:00}_lf.tga",
            EffectScnRelativePath + "skybox" + PathSeparator + "{0:00}" + PathSeparator + "{0:00}_fr.tga",
            EffectScnRelativePath + "skybox" + PathSeparator + "{0:00}" + PathSeparator + "{0:00}_rt.tga",
            EffectScnRelativePath + "skybox" + PathSeparator + "{0:00}" + PathSeparator + "{0:00}_bk.tga",
            EffectScnRelativePath + "skybox" + PathSeparator + "{0:00}" + PathSeparator + "{0:00}_up.tga",
            EffectScnRelativePath + "skybox" + PathSeparator + "{0:00}" + PathSeparator + "{0:00}_dn.tga",
        };
        
        #if PAL3
        public static readonly List<(string cpkName, string sceneName)> SceneCpkNameInfos = new()
        {
            ("M01.cpk", "璧山"),
            ("M02.cpk", "九龙坡"),
            ("M03.cpk", "大渡口"),
            ("M04.cpk", "船·长江"),
            ("M05.cpk", "船·海"),
            ("M06.cpk", "蓬莱"),
            ("m08.cpk", "九顶山"),
            ("M09.cpk", "霹雳堂总舵"),
            ("m10.cpk", "古藤林"),
            ("m11.cpk", "蜀山故道"),
            ("M15.cpk", "神魔之井"),
            ("M16.cpk", "神树"),
            ("M17.cpk", "锁妖塔"),
            ("M18.cpk", "草海"),
            ("M19.cpk", "灵山仙人洞"),
            ("M20.cpk", "熔岩地狱"),
            ("M21.cpk", "黄泉路"),
            ("M22.cpk", "冰风谷"),
            ("M23.cpk", "海底城"),
            ("M24.cpk", "剑冢"),
            ("M25.cpk", "新仙界"),
            ("M26.cpk", "星森"),
            ("Q01.cpk", "渝州"),
            ("Q02.cpk", "宾化"),
            ("Q03.cpk", "镇江"),
            ("Q04.cpk", "蓬莱御剑堂"),
            ("Q05.cpk", "唐家堡"),
            ("Q06.cpk", "德阳"),
            ("Q07.cpk", "安宁村"),
            ("Q08.cpk", "蜀山派"),
            ("Q09.cpk", "雷州"),
            ("Q10.cpk", "神界天门"),
            ("Q11.cpk", "蛮州"),
            ("Q12.cpk", "古城镇"),
            ("Q13.cpk", "酆都"),
            ("Q14.cpk", "鬼界外围"),
            ("Q15.cpk", "雪岭镇"),
            ("Q16.cpk", "安溪"),
            ("Q17.cpk", "船"),
            ("T01.cpk", ""),
            ("T02.cpk", ""),
        };
        #elif PAL3A
        public static readonly List<(string cpkName, string sceneName)> SceneCpkNameInfos = new()
        {
            ("m01.cpk",    "登云麓"),
            ("m02.cpk",    "地脉门户"),
            ("m03.cpk",    "绿萝嶂"),
            ("m04.cpk",    "少阳三焦"),
            ("m05.cpk",    "厥阴心包"),
            ("m06.cpk",    "纳林河源"),
            ("m07.cpk",    "阳名百纳"),
            ("m08.cpk",    "双溪"),
            ("m09.cpk",    "太阴归尘"),
            ("m10.cpk",    "蜀山故道"),
            ("m11.cpk",    "锁妖塔"),
            ("m12.cpk",    "太阳华池"),
            ("m13.cpk",    "魔界之门"),
            ("m14.cpk",    "少阴凝碧"),
            ("m15.cpk",    "月光城"),
            ("m16.cpk",    "少阳参天"),
            ("m17.cpk",    "厥阴蔽日"),
            ("m18.cpk",    "盘古之心"),
            ("m19.cpk",    "里蜀山内城"),
            ("m20.cpk",    "秘密储藏室"),
            ("m21.cpk",    "火灵珠祭坛"),
            ("q01.cpk",    "唐家堡"),
            ("q02.cpk",    "蜀山派"),
            ("q02_01.cpk", "蜀山派"),
            ("q02_02.cpk", "蜀山派"),
            ("q02_03.cpk", "蜀山派"),
            ("q03.cpk",    "绿萝山"),
            ("q04.cpk",    "里蜀山"),
            ("q05.cpk",    "胜州"),
            ("q06.cpk",    "京城"),
            ("q07.cpk",    "石村"),
            ("q08.cpk",    "蜀山故道大营"),
            ("q09.cpk",    "渝州"),
            ("q10.cpk",    "施洞"),
            ("q11.cpk",    "蜀山前山"),
            ("y01.cpk",    ""),
        };
        #endif
    }
}