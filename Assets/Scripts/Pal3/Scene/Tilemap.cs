// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Scene
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Core.Algorithm.PathFinding;
    using Core.DataReader.Nav;
    using Core.GameBox;
    using MetaData;
    using UnityEngine;

    public class Tilemap
    {
        private const int MOVE_COST_STRAIGHT = 10;
        private const int MOVE_COST_DIAGONAL = 14; // sqrt(200)

        private readonly NavFile _navFile;

        public Tilemap(NavFile navFile)
        {
            _navFile = navFile;
        }

        public int GetLayerCount()
        {
            return _navFile.TileLayers.Length;
        }

        public bool IsInsidePortalArea(Vector2Int position, int layerIndex)
        {
            NavTileLayer currentLayer = _navFile.TileLayers[layerIndex];
            return currentLayer.Portals.Any(portal =>
                GameBoxInterpreter.IsPositionInsideRect(portal, position));
        }

        public bool IsTilePositionInsideTileMap(Vector2Int tilePosition, int layerIndex)
        {
            return tilePosition.x >=0 && tilePosition.x < _navFile.TileLayers[layerIndex].Width &&
                   tilePosition.y >=0 && tilePosition.y < _navFile.TileLayers[layerIndex].Height;
        }

        public Vector3 GetWorldPosition(Vector2Int tilePosition, int layerIndex)
        {
            var isInside = IsTilePositionInsideTileMap(tilePosition, layerIndex);
            NavTileLayer currentLayer = _navFile.TileLayers[layerIndex];
            var position = new Vector3(
                currentLayer.Min.x + (tilePosition.x + 1/2f) * NavigationConstants.NavTileSize,
                isInside ? GetTile(tilePosition, layerIndex).GameBoxYPosition : 0f,
                currentLayer.Min.z + (tilePosition.y + 1/2f) * NavigationConstants.NavTileSize);
            return GameBoxInterpreter.ToUnityPosition(position);
        }

        public Vector2Int GetTilePosition(Vector3 position, int layerIndex)
        {
            Vector3 gameBoxPosition = GameBoxInterpreter.ToGameBoxPosition(position);
            NavTileLayer currentLayer = _navFile.TileLayers[layerIndex];
            return new Vector2Int(
                (int) ((gameBoxPosition.x - currentLayer.Min.x) / NavigationConstants.NavTileSize),
                (int) ((gameBoxPosition.z - currentLayer.Min.z) / NavigationConstants.NavTileSize));
        }

        public bool TryGetTile(Vector3 position, int layerIndex, out NavTile tile)
        {
            tile = default;
            Vector2Int tilePosition = GetTilePosition(position, layerIndex);
            if (!IsTilePositionInsideTileMap(tilePosition, layerIndex)) return false;
            tile = GetTile(tilePosition, layerIndex);
            return true;
        }

        private NavTile GetTile(Vector2Int position, int layerIndex)
        {
            NavTileLayer currentLayer = _navFile.TileLayers[layerIndex];
            return currentLayer.Tiles[position.x + position.y * currentLayer.Width];
        }

        public Vector2Int[] FindPathToTilePositionThreadSafe(Vector2Int from,
            Vector2Int to,
            int layerIndex,
            HashSet<Vector2Int> obstacles)
        {
            if (!IsTilePositionInsideTileMap(from, layerIndex))
            {
                Debug.LogWarning("From position is not inside tilemap bounds.");
                return Array.Empty<Vector2Int>();
            }

            if (!IsTilePositionInsideTileMap(to, layerIndex))
            {
                Debug.LogWarning("To position is not inside tilemap bounds.");
                return Array.Empty<Vector2Int>();
            }

            // In case "to" position is not walkable, we try to get a walkable tile right
            // next to the "to" tile to compensate (if there is any walkable tile right next to it).
            if (!GetTile(to, layerIndex).IsWalkable())
            {
                if (TryGetAdjacentWalkableTile(to, layerIndex, out Vector2Int nearestWalkableTile))
                {
                    to = nearestWalkableTile;
                }
                else
                {
                    Debug.LogWarning("To position is not walkable.");
                    return Array.Empty<Vector2Int>();
                }
            }

            var path = new List<Vector2Int>();
            NavTileLayer currentLayer = _navFile.TileLayers[layerIndex];

            bool IsObstacle(Vector2Int position)
            {
                // Check if position is blocked by predefined obstacles
                // Ignore obstacles nearest to from and to positions just to be safe
                if (Vector2Int.Distance(from, position) > 1 &&
                    Vector2Int.Distance(to, position) > 1 &&
                    obstacles.Contains(position)) return true;
                var index = position.x + position.y * currentLayer.Width;
                
                return !currentLayer.Tiles[index].IsWalkable();
            }

            int GetDistanceCost(Vector2Int fromTile, Vector2Int toTile)
            {
                int xDistance = Mathf.Abs(fromTile.x - toTile.x);
                int yDistance = Mathf.Abs(fromTile.y - toTile.y);
                int remaining = Mathf.Abs(xDistance - yDistance);
                return MOVE_COST_DIAGONAL * Mathf.Min(xDistance, yDistance) +
                       MOVE_COST_STRAIGHT * remaining +
                       GetTileExtraWeight(GetTile(toTile, layerIndex));
            }

            var result = LazyThetaStarPathFinder.FindPath(
                from,
                to,
                new Vector2Int(currentLayer.Width, currentLayer.Height),
                IsObstacle,
                GetDistanceCost);

            for (var i = 0; i < result.Count; i++)
            {
                if (i == 0 && result[i].x == from.x && result[i].y == from.y) continue;
                path.Add(new Vector2Int(result[i].x, result[i].y));
            }

            return path.ToArray();
        }

        // Get a walkable tile right next to the tile if any.
        public bool TryGetAdjacentWalkableTile(Vector2Int position, int layerIndex, out Vector2Int nearestWalkableTile)
        {
            foreach (Direction direction in Enum.GetValues(typeof(Direction)).Cast<Direction>())
            {
                Vector2Int tile = position + DirectionUtils.ToVector2Int(direction);
                if (!IsTilePositionInsideTileMap(tile, layerIndex)) continue;
                if (GetTile(tile, layerIndex).IsWalkable())
                {
                    nearestWalkableTile = tile;
                    return true;
                }
            }

            nearestWalkableTile = position;
            return false;
        }

        // Add some weights to the tiles near obstacle.
        private int GetTileExtraWeight(NavTile tile)
        {
            return tile.DistanceToNearestObstacle switch
            {
                1 => 20,
                2 => 10,
                3 => 5,
                4 => 0,
                5 => 0,
                6 => 0,
                7 => 0,
                _ => 0
            };
        }
    }
}