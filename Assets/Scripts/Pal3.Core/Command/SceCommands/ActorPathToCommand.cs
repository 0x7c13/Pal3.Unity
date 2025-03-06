// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2025, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Core.Command.SceCommands
{
    [SceCommand(20, "查找一个到TileMap上（X，Y）的路径，并使角色按此路径移动，" +
                    "参数：角色ID，TileMap中X坐标，TileMap中Y坐标，运动模式（0走，1跑，2后退）")]
    public sealed class ActorPathToCommand : ICommand
    {
        public ActorPathToCommand(int actorId,
            int tileXPosition,
            int tileYPosition,
            int mode)
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