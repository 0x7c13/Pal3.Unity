// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Core.DataReader.Ini
{
    using UnityEngine;

    public sealed class CombatConfigFile
    {
        public Vector3[] ActorGameBoxPositions { get; }

        public CombatConfigFile(Vector3[] actorGameBoxPositions)
        {
            ActorGameBoxPositions = actorGameBoxPositions;
        }
    }
}