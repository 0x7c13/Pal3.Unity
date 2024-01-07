// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2024, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Core.Command.SceCommands
{
    [SceCommand(214, "查找一个到TileMap上（X，Y）的路径，并使角色按此路径移动，移动中使用指定动作" +
                    "参数：角色ID，TileMap中X坐标，TileMap中Y坐标，动作类型（0走，1跑，2后退）")]
    public sealed class ActorMoveToCommand : ICommand
    {
        public ActorMoveToCommand(int actorId,
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