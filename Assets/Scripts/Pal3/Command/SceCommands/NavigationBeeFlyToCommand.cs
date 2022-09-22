// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Command.SceCommands
{
    [SceCommand(154, "控制引路蜂飞向指定位置" +
                     "参数：Nav层，TileMap中X坐标，TileMap中Z坐标")]
    public class NavigationBeeFlyToCommand : ICommand
    {
        public NavigationBeeFlyToCommand(
            int navLayerIndex,
            int tileXPosition,
            int tileZPosition)
        {
            NavLayerIndex = navLayerIndex;
            TileXPosition = tileXPosition;
            TileZPosition = tileZPosition;
        }
        
        public int NavLayerIndex { get; }
        public int TileXPosition { get; }
        public int TileZPosition { get; }
    }
}