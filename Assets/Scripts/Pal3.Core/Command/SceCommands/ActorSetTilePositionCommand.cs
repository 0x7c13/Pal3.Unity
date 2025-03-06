// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2025, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Core.Command.SceCommands
{
    [SceCommand(21, "瞬间移动角色到TileMap中该点，" +
                    "参数：角色ID，TileMap中X坐标，TileMap中Y坐标")]
    public sealed class ActorSetTilePositionCommand : ICommand
    {
        public ActorSetTilePositionCommand(int actorId,
            int tileXPosition,
            int tileYPosition)
        {
            ActorId = actorId;
            TileXPosition = tileXPosition;
            TileYPosition = tileYPosition;
        }

        [SceActorId] public int ActorId { get; set; }
        public int TileXPosition { get; }
        public int TileYPosition { get; }
    }
}