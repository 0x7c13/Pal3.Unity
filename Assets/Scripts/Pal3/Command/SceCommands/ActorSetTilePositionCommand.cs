// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Command.SceCommands
{
    [SceCommand(21, "瞬间移动角色到TileMap中该点，" +
                    "参数：角色ID，TileMap中X坐标，TileMap中Z坐标")]
    public class ActorSetTilePositionCommand : ICommand
    {
        public ActorSetTilePositionCommand(int actorId, int tileXPosition, int tileZPosition)
        {
            ActorId = actorId;
            TileXPosition = tileXPosition;
            TileZPosition = tileZPosition;
        }

        // 角色ID为-1时 (byte值就是255) 表示当前受玩家操作的主角
        public int ActorId { get; }
        public int TileXPosition { get; }
        public int TileZPosition { get; }
    }
}