// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Command.SceCommands
{
    [AvailableInConsole]
    [SceCommand(125, "大地图打开或关闭某个区域，" +
                     "参数：区域ID，0关闭，1显示但不可飞行，2显示且可以飞行")]
    public class WorldMapEnableRegionCommand : ICommand
    {
        public WorldMapEnableRegionCommand(int region, int enablementFlag)
        {
            Region = region;
            EnablementFlag = enablementFlag;
        }

        public int Region { get; }
        public int EnablementFlag { get; }
    }
}