// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE.txt in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Command.InternalCommands
{
    using UnityEngine;

    public class PlayerActorPositionUpdatedNotification : ICommand
    {
        public PlayerActorPositionUpdatedNotification(Vector3 position, int layerIndex, bool movedByScript)
        {
            Position = position;
            LayerIndex = layerIndex;
            MovedByScript = movedByScript;
        }

        public Vector3 Position { get; }
        public int LayerIndex { get; }
        public bool MovedByScript { get; }
    }
}