// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Command.SceCommands
{
    #if PAL3A
    [AvailableInConsole]
    [SceCommand(179, "查找一个到TileMap上（X，Z）的路径，并使角色按此路径走动，" +
                    "参数：角色ID，TileMap中X坐标，TileMap中Z坐标，动作名")]
    public class ActorWalkToUsingActionCommand : ICommand
    {
        public ActorWalkToUsingActionCommand(int actorId, int tileX, int tileZ, string action)
        {
            ActorId = actorId;
            TileX = tileX;
            TileZ = tileZ;
            Action = action;
        }

        // 角色ID为-1时 (byte值就是255) 表示当前受玩家操作的主角
        public int ActorId { get; }
        public int TileX { get; }
        public int TileZ { get; }
        public string Action { get; }
    }
    #endif
}