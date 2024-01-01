// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2024, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Core.Command.SceCommands
{
    [SceCommand(201, "使角色向指定位置走,直到走出屏幕（不一定走到指定的点），" +
                    "参数：角色ID，TileMap中X坐标，TileMap中Y坐标，运动模式（0走，1跑）")]
    public sealed class ActorMoveOutOfScreenCommand : ICommand
    {
        public ActorMoveOutOfScreenCommand(int actorId, int tileXPosition, int tileYPosition, int mode)
        {
            ActorId = actorId;
            TileXPosition = tileXPosition;
            TileYPosition = tileYPosition;
            Mode = mode;
        }

        [SceActorId] public int ActorId { get; set; }
        public int TileXPosition { get; }
        public int TileYPosition { get; }
        public int Mode { get; }
    }
}