// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2024, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Core.Command.SceCommands
{
    [SceCommand(154, "控制引路蜂飞向指定位置" +
                     "参数：Nav层，TileMap中X坐标，TileMap中Y坐标")]
    public sealed class NavigationBeeFlyToCommand : ICommand
    {
        public NavigationBeeFlyToCommand(
            int navLayerIndex,
            int tileXPosition,
            int tileYPosition)
        {
            NavLayerIndex = navLayerIndex;
            TileXPosition = tileXPosition;
            TileYPosition = tileYPosition;
        }

        public int NavLayerIndex { get; }
        public int TileXPosition { get; }
        public int TileYPosition { get; }
    }
}