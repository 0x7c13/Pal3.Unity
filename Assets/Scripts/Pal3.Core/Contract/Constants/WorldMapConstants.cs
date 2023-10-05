// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Core.Contract.Constants
{
    using System.Collections.Generic;
    using Primitives;

    public static class WorldMapConstants
    {
        #if PAL3
        // Order matters
        public static readonly string[] WorldMapRegions = new[]
        {
            "安宁村",
            "安溪",
            "宾化",
            "壁山",
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
        #elif PAL3A
        // Order matters
        public static string[] WorldMapRegions = new[]
        {
            "唐家堡",
            "登云麓",
            "蜀山",
            "NONE",
            "绿萝嶂",
            "绿萝山",
            "里蜀山",
            "胜州",
            "纳林河源",
            "京城",
            "石村",
            "蜀山故道",
            "锁妖塔",
            "渝州",
            "施洞",
            "月光城",
        };
        #endif

        // From basedata.cpk\ui\BigMap\Element\色表.doc
        public static readonly Dictionary<string, Color32> WorldMapRegionColorInfo = new()
        {
            {"安宁村", new Color32(115, 81, 53)},
            {"船长江", new Color32(48, 94, 95)},
            {"古藤林", new Color32(62, 87, 50)},
            {"九龙坡", new Color32(75, 87, 45)},
            {"安溪", new Color32(52, 97, 69)},
            {"船海", new Color32(48, 72, 94)},
            {"鬼界外围", new Color32(50, 78, 101)},
            {"雷洲", new Color32(113, 85, 51)},
            {"壁山", new Color32(74, 99, 53)},
            {"大渡口", new Color32(47, 73, 44)},
            {"海底城", new Color32(52, 101, 74)},
            {"蛮洲", new Color32(76, 95, 46)},
            {"宾化", new Color32(144, 90, 47)},
            {"德阳", new Color32(113, 80, 58)},
            {"黄泉路", new Color32(154, 79, 61)},
            {"魔幻空间", new Color32(119, 108, 55)},
            {"冰峰谷", new Color32(52, 87, 102)},
            {"酆都", new Color32(55, 91, 50)},
            {"剑冢", new Color32(53, 78, 130)},
            {"蓬莱", new Color32(56, 116, 106)},
            {"草海", new Color32(52, 97, 65)},
            {"古城镇", new Color32(52, 52, 91)},
            {"九顶山", new Color32(60, 93, 51)},
            {"蓬莱迷宫", new Color32(57, 128, 105)},
            {"霹雳堂", new Color32(49, 83, 97)},
            {"熔岩地狱", new Color32(133, 90, 61)},
            {"神界天门", new Color32(87, 77, 44)},
            {"神树", new Color32(80, 93, 46)},
            {"蜀山", new Color32(93, 76, 40)},
            {"蜀山故道", new Color32(92, 70, 45)},
            {"锁妖塔", new Color32(100, 79, 46)},
            {"堂家堡", new Color32(100, 81, 55)},
            {"仙人洞", new Color32(73, 88, 48)},
            {"新仙界", new Color32(49, 95, 90)},
            {"星森", new Color32(81, 110, 58)},
            {"雪岭镇", new Color32(51, 71, 95)},
            {"渝洲", new Color32(48, 85, 91)},
            {"镇江", new Color32(51, 97, 82)},
        };
    }
}