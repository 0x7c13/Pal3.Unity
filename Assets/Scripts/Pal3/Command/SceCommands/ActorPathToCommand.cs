// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Command.SceCommands
{
    [SceCommand(20, "查找一个到TileMap上（X，Z）的路径，并使角色按此路径移动，" +
                    "参数：角色ID，TileMap中X坐标，TileMap中Z坐标，运动模式（0走，1跑，2后退）")]
    public class ActorPathToCommand : ICommand
    {
        public ActorPathToCommand(int actorId, int tileX, int tileZ, int mode)
        {
            ActorId = actorId;
            TileX = tileX;
            TileZ = tileZ;
            Mode = mode;
        }

        // 角色ID为-1时 (byte值就是255) 表示当前受玩家操作的主角
        public int ActorId { get; }
        public int TileX { get; }
        public int TileZ { get; }
        public int Mode { get; }
    }
}