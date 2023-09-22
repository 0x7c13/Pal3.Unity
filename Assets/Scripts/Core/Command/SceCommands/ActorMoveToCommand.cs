// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Core.Command.SceCommands
{
    [SceCommand(214, "查找一个到TileMap上（X，Y）的路径，并使角色按此路径移动，移动中使用指定动作" +
                    "参数：角色ID，TileMap中X坐标，TileMap中Y坐标，动作类型（0走，1跑，2后退）")]
    public class ActorMoveToCommand : ICommand
    {
        public ActorMoveToCommand(int actorId, int tileXPosition, int tileYPosition, int mode)
        {
            ActorId = actorId;
            TileXPosition = tileXPosition;
            TileYPosition = tileYPosition;
            Mode = mode;
        }

        // 角色ID为-1时 (byte值就是255) 表示当前受玩家操作的主角
        public int ActorId { get; }
        public int TileXPosition { get; }
        public int TileYPosition { get; }
        public int Mode { get; }
    }
}