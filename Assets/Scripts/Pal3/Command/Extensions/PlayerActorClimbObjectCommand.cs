// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Command.Extensions
{
    using Core.Command;
    using UnityEngine;

    [AvailableInConsole]
    public class PlayerActorClimbObjectCommand : ICommand
    {
        public PlayerActorClimbObjectCommand(int objectId,
            Vector2Int fromPosition,
            Vector2Int toPosition,
            bool crossLayer)
        {
            ObjectId = objectId;
            FromPosition = fromPosition;
            ToPosition = toPosition;
            CrossLayer = crossLayer;
        }

        public int ObjectId { get; }
        public Vector2Int FromPosition { get; }
        public Vector2Int ToPosition { get; }
        public bool CrossLayer { get; }
    }
}