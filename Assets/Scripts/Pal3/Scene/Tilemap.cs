// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Scene
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Core.Algorithm;
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
            var currentLayer = _navFile.TileLayers[layerIndex];
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
            var currentLayer = _navFile.TileLayers[layerIndex];
            var position = new Vector3(
                currentLayer.Min.x + (tilePosition.x + 1/2f) * NavigationConstants.NavTileSize,
                isInside ? GetTile(tilePosition, layerIndex).Y : 0f,
                currentLayer.Min.z + (tilePosition.y + 1/2f) * NavigationConstants.NavTileSize);
            return GameBoxInterpreter.ToUnityPosition(position);
        }

        public Vector2Int GetTilePosition(Vector3 position, int layerIndex)
        {
            var gameBoxPosition = GameBoxInterpreter.ToGameBoxPosition(position);
            var currentLayer = _navFile.TileLayers[layerIndex];
            return new Vector2Int(
                (int) ((gameBoxPosition.x - currentLayer.Min.x) / NavigationConstants.NavTileSize),
                (int) ((gameBoxPosition.z - currentLayer.Min.z) / NavigationConstants.NavTileSize));
        }

        public NavTile GetTile(Vector3 position, int layerIndex)
        {
            var currentLayer = _navFile.TileLayers[layerIndex];
            var tilePosition = GetTilePosition(position, layerIndex);
            return currentLayer.Tiles[tilePosition.x + tilePosition.y * currentLayer.Width];
        }

        public NavTile GetTile(Vector2Int position, int layerIndex)
        {
            var currentLayer = _navFile.TileLayers[layerIndex];
            return currentLayer.Tiles[position.x + position.y * currentLayer.Width];
        }

        public Vector2Int[] FindPathToTilePosition(Vector2Int from, Vector2Int to, int layerIndex)
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
            // next to the "to" tile to compensate (if there is any walkable tile right next to it)
            if (!GetTile(to, layerIndex).IsWalkable())
            {
                if (TryGetAdjacentWalkableTile(to, layerIndex, out var nearestWalkableTile))
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
            var walkableMap = new Dictionary<int, bool>();

            var currentLayer = _navFile.TileLayers[layerIndex];

            for (var i = 0; i < currentLayer.Tiles.Length; i++)
            {
                walkableMap[i] = currentLayer.Tiles[i].IsWalkable();
            }

            int GetDistanceCost(Vector2Int fromTile, Vector2Int toTile)
            {
                int xDistance = Mathf.Abs(fromTile.x - toTile.x);
                int yDistance = Mathf.Abs(fromTile.y - toTile.y);
                int remaining = Mathf.Abs(xDistance - yDistance);
                return MOVE_COST_DIAGONAL * Mathf.Min(xDistance, yDistance) +
                       MOVE_COST_STRAIGHT * remaining +
                       GetTileWeight(GetTile(toTile, layerIndex));
            }

            foreach (var node in AStarPathfinder.FindPath(
                         from,
                         to,
                         new Vector2Int(currentLayer.Width, currentLayer.Height),
                         walkableMap,
                         GetDistanceCost))
            {
                path.Add(new Vector2Int(node.x, node.y));
            }

            return path.ToArray();
        }

        // Get a walkable tile right next to the tile if any
        public bool TryGetAdjacentWalkableTile(Vector2Int position, int layerIndex, out Vector2Int nearestWalkableTile)
        {
            foreach (var direction in Enum.GetValues(typeof(Direction)).Cast<Direction>())
            {
                var tile = position + DirectionUtils.ToVector2Int(direction);
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

        // Add some weights to the tiles near obstacle
        private int GetTileWeight(NavTile tile)
        {
            return tile.Distance switch
            {
                1 => 10,
                2 => 5,
                3 => 2,
                _ => 0
            };
        }
    }
}