// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2024, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Game.Command.Extensions
{
    using Core.Command;

    public sealed class PlayerActorTilePositionUpdatedNotification : ICommand
    {
        public PlayerActorTilePositionUpdatedNotification(
            int tileXPosition,
            int tileYPosition,
            int layerIndex,
            bool movedByScript)
        {
            TileXPosition = tileXPosition;
            TileYPosition = tileYPosition;
            LayerIndex = layerIndex;
            MovedByScript = movedByScript;
        }

        public int TileXPosition { get; }
        public int TileYPosition { get; }
        public int LayerIndex { get; }
        public bool MovedByScript { get; }
    }
}