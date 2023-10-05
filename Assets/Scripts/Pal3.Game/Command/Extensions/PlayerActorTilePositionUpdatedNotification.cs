// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Game.Command.Extensions
{
    using Core.Command;
    using UnityEngine;

    public class PlayerActorTilePositionUpdatedNotification : ICommand
    {
        public PlayerActorTilePositionUpdatedNotification(Vector2Int position, int layerIndex, bool movedByScript)
        {
            Position = position;
            LayerIndex = layerIndex;
            MovedByScript = movedByScript;
        }

        public Vector2Int Position { get; }
        public int LayerIndex { get; }
        public bool MovedByScript { get; }
    }
}