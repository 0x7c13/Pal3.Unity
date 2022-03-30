// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Core.Algorithm.PathFinding
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using UnityEngine;

    internal struct Node
    {
        public int X;
        public int Y;

        public int Index;

        public int GCost;
        public int HCost;
        public int FCost;

        public bool IsObstacle;

        public int PreviousNodeIndex;

        public void CalculateFCost()
        {
            FCost = GCost + HCost;
        }
    }

    /// <summary>
    /// A* path finding algorithm.
    /// </summary>
    public static class AStarPathFinder
    {
        public static IList<Vector2Int> FindPath(
            Vector2Int from,
            Vector2Int to,
            Vector2Int gridSize,
            Func<Vector2Int, bool> isObstacle,
            Func<Vector2Int, Vector2Int, int> heuristicFunc)
        {
            var pathResult = new List<Vector2Int>();
            Span<Node> nodeArray = new Node[gridSize.x * gridSize.y];

            // Init
            for (var x = 0; x < gridSize.x; x++)
            {
                for (var y = 0; y < gridSize.y; y++)
                {
                    var position = new Vector2Int(x, y);
                    var node = new Node
                    {
                        X = x,
                        Y = y,
                        Index = GetIndex(x, y, gridSize.x),
                        GCost = int.MaxValue,
                        HCost = heuristicFunc(position, to)
                    };
                    node.CalculateFCost();
                    node.IsObstacle = isObstacle(position);
                    node.PreviousNodeIndex = -1;
                    nodeArray[node.Index] = node;
                }
            }

            // Init start node
            var startNode = nodeArray[GetIndex(from.x, from.y, gridSize.x)];
            startNode.GCost = 0;
            startNode.CalculateFCost();
            nodeArray[startNode.Index] = startNode;

            var endNodeIndex = GetIndex(to.x, to.y, gridSize.x);

            List<int> openList = new List<int>();
            List<int> closedList = new List<int>();

            Vector2Int[] neighbourOffset = new Vector2Int[8];
            neighbourOffset[0] = new Vector2Int(+0, -1);
            neighbourOffset[1] = new Vector2Int(+0, +1);
            neighbourOffset[2] = new Vector2Int(-1, +0);
            neighbourOffset[3] = new Vector2Int(+1, +0);
            neighbourOffset[4] = new Vector2Int(-1, -1);
            neighbourOffset[5] = new Vector2Int(+1, +1);
            neighbourOffset[6] = new Vector2Int(+1, -1);
            neighbourOffset[7] = new Vector2Int(-1, +1);

            openList.Add(startNode.Index);

            while (openList.Count > 0)
            {
                var currentNodeIndex = GetLowestCostFNodeIndex(openList, nodeArray);
                var currentNode = nodeArray[currentNodeIndex];
                if (currentNodeIndex == endNodeIndex)
                {
                    break;
                }

                for (var i = 0; i < openList.Count; i++)
                {
                    if (openList[i] == currentNodeIndex)
                    {
                        openList[i] = openList.Last();
                        openList.RemoveAt(openList.Count - 1);
                        break;
                    }
                }

                closedList.Add(currentNodeIndex);

                for (var i = 0; i < neighbourOffset.Length; i++)
                {
                    var offset = neighbourOffset[i];
                    var neighbourPosition = new Vector2Int(currentNode.X + offset.x, currentNode.Y + offset.y);

                    if (!IsPositionInsideGrid(neighbourPosition, gridSize))
                    {
                        continue;
                    }

                    var neighbourNodeIndex = GetIndex(neighbourPosition.x, neighbourPosition.y, gridSize.x);

                    if (closedList.Contains(neighbourNodeIndex))
                    {
                        continue;
                    }

                    var neighbourNode = nodeArray[neighbourNodeIndex];
                    if (neighbourNode.IsObstacle)
                    {
                        continue;
                    }

                    var currentNodePosition = new Vector2Int(currentNode.X, currentNode.Y);

                    var tentativeGCost = currentNode.GCost + heuristicFunc(currentNodePosition, neighbourPosition);
                    if (tentativeGCost < neighbourNode.GCost)
                    {
                        neighbourNode.PreviousNodeIndex = currentNodeIndex;
                        neighbourNode.GCost = tentativeGCost;
                        neighbourNode.CalculateFCost();
                        nodeArray[neighbourNodeIndex] = neighbourNode;

                        if (!openList.Contains(neighbourNode.Index))
                        {
                            openList.Add(neighbourNode.Index);
                        }
                    }
                }
            }

            var endNode = nodeArray[endNodeIndex];
            if (endNode.PreviousNodeIndex != -1)
            {
                var pathList = GetPath(nodeArray, endNode);
                for (var i = pathList.Count - 1; i >= 0; i--)
                {
                    pathResult.Add(new Vector2Int(pathList[i].x, pathList[i].y));
                }
            }

            return pathResult;
        }

        private static int GetIndex(int x, int y, int gridWidth)
        {
            return x + y * gridWidth;
        }

        private static int GetLowestCostFNodeIndex(IList<int> openList, ReadOnlySpan<Node> nodeArray)
        {
            var lowestCostNode = nodeArray[openList[0]];
            for (var i = 0; i < openList.Count; i++)
            {
                var currentNode = nodeArray[openList[i]];
                if (currentNode.FCost < lowestCostNode.FCost)
                {
                    lowestCostNode = currentNode;
                }
            }

            return lowestCostNode.Index;
        }

        private static bool IsPositionInsideGrid(Vector2Int position, Vector2Int gridSize)
        {
            return position.x >= 0 &&
                   position.y >= 0 &&
                   position.x < gridSize.x &&
                   position.y < gridSize.y;
        }

        private static IList<Vector2Int> GetPath(ReadOnlySpan<Node> nodeArray, Node endNode)
        {
            if (endNode.PreviousNodeIndex == -1)
            {
                return new List<Vector2Int>();
            }
            else
            {
                List<Vector2Int> path = new () { new Vector2Int(endNode.X, endNode.Y) };
                var currentNode = endNode;
                while (currentNode.PreviousNodeIndex != -1)
                {
                    var previousNode = nodeArray[currentNode.PreviousNodeIndex];
                    path.Add(new Vector2Int(previousNode.X, previousNode.Y));
                    currentNode = previousNode;
                }

                return path;
            }
        }
    }
}