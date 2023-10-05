// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Core.Command.SceCommands
{
    #if PAL3A
    [SceCommand(179, "查找一个到TileMap上（X，Y）的路径，并使角色按此路径走动，" +
                    "参数：角色ID，TileMap中X坐标，TileMap中Y坐标，动作名")]
    public class ActorWalkToUsingActionCommand : ICommand
    {
        public ActorWalkToUsingActionCommand(int actorId, int tileXPosition, int tileYPosition, string action)
        {
            ActorId = actorId;
            TileXPosition = tileXPosition;
            TileYPosition = tileYPosition;
            Action = action;
        }

        // 角色ID为-1时 (byte值就是255) 表示当前受玩家操作的主角
        public int ActorId { get; }
        public int TileXPosition { get; }
        public int TileYPosition { get; }
        public string Action { get; }
    }
    #endif
}