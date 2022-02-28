// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE.txt in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.MetaData
{
    using System.Collections.Generic;
    using UnityEngine;

    public static class BigMapConstants
    {
        // Order matters
        public static string[] BigMapRegions = new[]
        {
            "安宁村",
            "安溪",
            "滨化",
            "碧山",
            "冰风谷",
            "草海",
            "船长江",
            "船海",
            "大渡口",
            "德阳",
            "酆都",
            "古城镇",
            "古藤林",
            "鬼界外围",
            "海底城",
            "黄泉路",
            "剑冢",
            "九顶山",
            "九龙坡",
            "雷州",
            "蛮州",
            "神魔之井",
            "蓬莱",
            "蓬莱迷宫",
            "霹雳堂",
            "熔岩地狱",
            "神界天门",
            "神树",
            "蜀山",
            "蜀山故道",
            "锁妖塔",
            "唐家堡",
            "仙人洞",
            "新仙界",
            "星森",
            "雪岭镇",
            "渝州",
            "镇江"
        };

        // From basedata.cpk\ui\BigMap\Element\色表.doc
        public static readonly Dictionary<string, Color32> BigMapRegionColorInfo = new()
        {
            {"安宁村", new Color(115, 81, 53)},
            {"船长江", new Color(48, 94, 95)},
            {"古藤林", new Color(62, 87, 50)},
            {"九龙坡", new Color(75, 87, 45)},
            {"安溪", new Color(52, 97, 69)},
            {"船海", new Color(48, 72, 94)},
            {"鬼界外围", new Color(50, 78, 101)},
            {"雷洲", new Color(113, 85, 51)},
            {"碧山", new Color(74, 99, 53)},
            {"大渡口", new Color(47, 73, 44)},
            {"海底城", new Color(52, 101, 74)},
            {"蛮洲", new Color(76, 95, 46)},
            {"宾化", new Color(144, 90, 47)},
            {"德阳", new Color(113, 80, 58)},
            {"黄泉路", new Color(154, 79, 61)},
            {"魔幻空间", new Color(119, 108, 55)},
            {"冰峰谷", new Color(52, 87, 102)},
            {"酆都", new Color(55, 91, 50)},
            {"剑冢", new Color(53, 78, 130)},
            {"蓬莱", new Color(56, 116, 106)},
            {"草海", new Color(52, 97, 65)},
            {"古城镇", new Color(52, 52, 91)},
            {"九顶山", new Color(60, 93, 51)},
            {"蓬莱迷宫", new Color(57, 128, 105)},
            {"霹雳堂", new Color(49, 83, 97)},
            {"熔岩地狱", new Color(133, 90, 61)},
            {"神界天门", new Color(87, 77, 44)},
            {"神树", new Color(80, 93, 46)},
            {"蜀山", new Color(93, 76, 40)},
            {"蜀山故道", new Color(92, 70, 45)},
            {"锁妖塔", new Color(100, 79, 46)},
            {"堂家堡", new Color(100, 81, 55)},
            {"仙人洞", new Color(73, 88, 48)},
            {"新仙界", new Color(49, 95, 90)},
            {"星森", new Color(81, 110, 58)},
            {"雪岭镇", new Color(51, 71, 95)},
            {"渝洲", new Color(48, 85, 91)},
            {"镇江", new Color(51, 97, 82)},
        };
    }
}