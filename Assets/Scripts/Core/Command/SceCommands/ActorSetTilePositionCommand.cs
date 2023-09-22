// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Core.Command.SceCommands
{
    [SceCommand(21, "瞬间移动角色到TileMap中该点，" +
                    "参数：角色ID，TileMap中X坐标，TileMap中Y坐标")]
    public class ActorSetTilePositionCommand : ICommand
    {
        public ActorSetTilePositionCommand(int actorId, int tileXPosition, int tileYPosition)
        {
            ActorId = actorId;
            TileXPosition = tileXPosition;
            TileYPosition = tileYPosition;
        }

        // 角色ID为-1时 (byte值就是255) 表示当前受玩家操作的主角
        public int ActorId { get; }
        public int TileXPosition { get; }
        public int TileYPosition { get; }
    }
}