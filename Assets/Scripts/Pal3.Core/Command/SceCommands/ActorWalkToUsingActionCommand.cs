// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2024, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Core.Command.SceCommands
{
    #if PAL3A
    [SceCommand(179, "查找一个到TileMap上（X，Y）的路径，并使角色按此路径走动，" +
                    "参数：角色ID，TileMap中X坐标，TileMap中Y坐标，动作名")]
    public sealed class ActorWalkToUsingActionCommand : ICommand
    {
        public ActorWalkToUsingActionCommand(int actorId,
             int tileXPosition,
             int tileYPosition,
             string action)
        {
            ActorId = actorId;
            TileXPosition = tileXPosition;
            TileYPosition = tileYPosition;
            Action = action;
        }

        [SceActorId] public int ActorId { get; set; }
        public int TileXPosition { get; }
        public int TileYPosition { get; }
        public string Action { get; }
    }
    #endif
}