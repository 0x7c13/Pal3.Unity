// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2024, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Game.Scene
{
    using System;
    using System.Collections.Generic;
    using Core.Algorithm.PathFinding;
    using Core.Contract.Enums;
    using Core.DataReader.Nav;
    using Core.Primitives;
    using Engine.Extensions;
    using Engine.Logging;
    using Engine.Navigation;

    using Vector2Int = UnityEngine.Vector2Int;
    using Vector3 = UnityEngine.Vector3;

    public sealed class Tilemap
    {
        // GameBox Tile size to Unity unit size ratio
        private const float NAV_TILE_SIZE = 12f;

        private const int MOVE_COST_STRAIGHT = 10;
        private const int MOVE_COST_DIAGONAL = 14; // sqrt(200)

        private readonly NavFile _navFile;

        public Tilemap(NavFile navFile)
        {
            _navFile = navFile;
        }

        public int GetLayerCount()
        {
            return _navFile.Layers.Length;
        }

        public bool IsTilePositionInsideTileMap(Vector2Int tilePosition, int layerIndex)
        {
            return tilePosition.x >=0 && tilePosition.x < _navFile.Layers[layerIndex].Width &&
                   tilePosition.y >=0 && tilePosition.y < _navFile.Layers[layerIndex].Height;
        }

        public Vector3 GetWorldPosition(Vector2Int tilePosition, int layerIndex)
        {
            bool isInside = IsTilePositionInsideTileMap(tilePosition, layerIndex);
            NavLayer currentLayer = _navFile.Layers[layerIndex];
            GameBoxVector3 position = new(
                currentLayer.GameBoxMinWorldPosition.X + (tilePosition.x + 1/2f) * NAV_TILE_SIZE,
                isInside ? GetTile(tilePosition, layerIndex).GameBoxYPosition : 0f,
                currentLayer.GameBoxMinWorldPosition.Z + (tilePosition.y + 1/2f) * NAV_TILE_SIZE);
            return position.ToUnityPosition();
        }

        public Vector2Int GetTilePosition(Vector3 position, int layerIndex)
        {
            GameBoxVector3 gameBoxPosition = position.ToGameBoxPosition();
            NavLayer currentLayer = _navFile.Layers[layerIndex];
            return new Vector2Int(
                (int) ((gameBoxPosition.X - currentLayer.GameBoxMinWorldPosition.X) / NAV_TILE_SIZE),
                (int) ((gameBoxPosition.Z - currentLayer.GameBoxMinWorldPosition.Z) / NAV_TILE_SIZE));
        }

        public bool TryGetTile(Vector3 position, int layerIndex, out NavTile tile)
        {
            tile = default;
            Vector2Int tilePosition = GetTilePosition(position, layerIndex);
            return TryGetTile(tilePosition, layerIndex, out tile);
        }

        public bool TryGetTile(Vector2Int tilePosition, int layerIndex, out NavTile tile)
        {
            tile = default;
            if (!IsTilePositionInsideTileMap(tilePosition, layerIndex)) return false;
            tile = GetTile(tilePosition, layerIndex);
            return true;
        }

        private NavTile GetTile(Vector2Int position, int layerIndex)
        {
            NavLayer currentLayer = _navFile.Layers[layerIndex];
            return currentLayer.Tiles[position.x + position.y * currentLayer.Width];
        }

        public NavLayer GetLayer(int layerIndex)
        {
            return _navFile.Layers[layerIndex];
        }

        public bool IsPositionInsidePortalArea(Vector3 position, int layerIndex)
        {
            Vector2Int tilePosition = GetTilePosition(position, layerIndex);

            NavLayer currentLayer = _navFile.Layers[layerIndex];

            foreach (GameBoxRect portalRect in currentLayer.Portals)
            {
                // The original game gives two adjacent portals a 1 unit gap,
                // so we need to smartly tweak the portal rect to make sure
                // the gap is removed, so that player can walk between two
                // adjacent portals freely.

                int horizontalLength = portalRect.Right - portalRect.Left;
                int verticalLength = portalRect.Bottom - portalRect.Top;

                GameBoxRect tweakedRect = new()
                {
                    Left = horizontalLength > verticalLength ? portalRect.Left : portalRect.Left - 1,
                    Right = horizontalLength > verticalLength ? portalRect.Right : portalRect.Right + 1,
                    Top = horizontalLength < verticalLength ? portalRect.Top : portalRect.Top - 1,
                    Bottom = horizontalLength < verticalLength ? portalRect.Bottom : portalRect.Bottom + 1
                };

                if (tweakedRect.IsPointInsideRect(tilePosition.x, tilePosition.y)) return true;
            }

            return false;
        }

        public Vector2Int[] FindPathToTilePositionThreadSafe(Vector2Int from,
            Vector2Int to,
            int layerIndex,
            HashSet<Vector2Int> obstacles)
        {
            if (!IsTilePositionInsideTileMap(from, layerIndex))
            {
                EngineLogger.LogWarning("[From] position is not inside tilemap bounds");
                return Array.Empty<Vector2Int>();
            }

            if (!IsTilePositionInsideTileMap(to, layerIndex))
            {
                EngineLogger.LogWarning("[To] position is not inside tilemap bounds");
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
                    EngineLogger.LogWarning("[To] position is not walkable");
                    return Array.Empty<Vector2Int>();
                }
            }

            List<Vector2Int> path = new ();
            NavLayer currentLayer = _navFile.Layers[layerIndex];

            bool IsObstacle((int x, int y) position)
            {
                Vector2Int pos = new Vector2Int(position.x, position.y);

                // Check if position is blocked by predefined obstacles
                // Ignore obstacles nearest to from and to positions just to be safe
                if (Vector2Int.Distance(from, pos) > 1 &&
                    Vector2Int.Distance(to, pos) > 1 &&
                    obstacles.Contains(pos)) return true;
                int index = position.x + position.y * currentLayer.Width;

                return !currentLayer.Tiles[index].IsWalkable();
            }

            int GetDistanceCost((int x, int y) fromTile, (int x, int y) toTile)
            {
                int xDistance = Math.Abs(fromTile.x - toTile.x);
                int yDistance = Math.Abs(fromTile.y - toTile.y);
                int remaining = Math.Abs(xDistance - yDistance);
                return MOVE_COST_DIAGONAL * Math.Min(xDistance, yDistance) +
                       MOVE_COST_STRAIGHT * remaining +
                       GetTileExtraWeight(GetTile(new Vector2Int(toTile.x, toTile.y), layerIndex));
            }

            IList<(int x, int y)> result = LazyThetaStarPathFinder.FindPath(
                (from.x, from.y),
                (to.x, to.y),
                (currentLayer.Width, currentLayer.Height),
                IsObstacle,
                GetDistanceCost);

            for (int i = 0; i < result.Count; i++)
            {
                if (i == 0 && result[i].x == from.x && result[i].y == from.y) continue;
                path.Add(new Vector2Int(result[i].x, result[i].y));
            }

            return path.ToArray();
        }

        // Get a walkable tile right next to the tile if any.
        public bool TryGetAdjacentWalkableTile(Vector2Int position, int layerIndex, out Vector2Int nearestWalkableTile)
        {
            foreach (Direction direction in DirectionUtils.AllDirections)
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

        public void MarkFloorTypeAsObstacle(FloorType floorType, bool isObstacle)
        {
            for (int i = 0; i < _navFile.Layers.Length; i++)
            {
                NavTile[] newTiles = new NavTile[_navFile.Layers[i].Tiles.Length];

                for (int j = 0; j < _navFile.Layers[i].Tiles.Length; j++)
                {
                    NavTile navTile = _navFile.Layers[i].Tiles[j];

                    if (navTile.FloorType == floorType)
                    {
                        navTile.IsObstacle = isObstacle;
                    }

                    newTiles[j] = navTile;
                }

                _navFile.Layers[i].Tiles = newTiles;
            }
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