// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2024, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Engine.Navigation
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Vector2Int = UnityEngine.Vector2Int;

    public static class DirectionUtils
    {
        public static IEnumerable<Direction> AllDirections => Enum.GetValues(typeof(Direction)).Cast<Direction>();

        public static Direction GetDirectionFromVector2Int(Vector2Int vector)
        {
            return (vector.x, vector.y) switch
            {
                (+0, +1) => Direction.North,
                (+1, +1) => Direction.NorthEast,
                (+1, +0) => Direction.East,
                (+1, -1) => Direction.SouthEast,
                (+0, -1) => Direction.South,
                (-1, -1) => Direction.SouthWest,
                (-1, +0) => Direction.West,
                (-1, +1) => Direction.NorthWest,
                _ => throw new ArgumentOutOfRangeException(nameof(vector), vector, null)
            };
        }

        public static Vector2Int ToVector2Int(Direction direction)
        {
            return direction switch
            {
                Direction.North      => new Vector2Int(+0, +1),
                Direction.NorthEast  => new Vector2Int(+1, +1),
                Direction.East       => new Vector2Int(+1, +0),
                Direction.SouthEast  => new Vector2Int(+1, -1),
                Direction.South      => new Vector2Int(+0, -1),
                Direction.SouthWest  => new Vector2Int(-1, -1),
                Direction.West       => new Vector2Int(-1, +0),
                Direction.NorthWest  => new Vector2Int(-1, +1),
                _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, null)
            };
        }
    }
}