// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

// AStar path finding job using UNITY DOTS
#if USE_UNITY_ECS_PACKAGE

using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace Core.Algorithm
{
    struct Node
    {
        public int X;
        public int Y;

        public int Index;

        public int GCost;
        public int HCost;
        public int FCost;

        public bool IsWalkable;

        public int PreviousNodeIndex;

        public void CalculateFCost()
        {
            FCost = GCost + HCost;
        }
    }

    [BurstCompile]
    public struct AStarPathfindingJob : IJob
    {
        private const int MOVE_COST_STRAIGHT = 10;
        private const int MOVE_COST_DIAGONAL = 14;

        public int2 From;
        public int2 To;
        public int2 GridSize;
        public NativeHashMap<int, bool> WalkableMap; // Key = X + Y * GridWidth, value = IsWalkable
        public NativeList<int2> PathResult;

        public void Execute()
        {
            NativeArray<Node> nodeArray = new NativeArray<Node>(GridSize.x * GridSize.y, Allocator.Temp);

            // Init
            for (var x = 0; x < GridSize.x; x++)
            {
                for (var y = 0; y < GridSize.y; y++)
                {
                    var node = new Node
                    {
                        X = x,
                        Y = y,
                        Index = GetIndex(x, y, GridSize.x),
                        GCost = int.MaxValue,
                        HCost = GetDistanceCost(new int2(x, y), To)
                    };
                    node.CalculateFCost();
                    node.IsWalkable = WalkableMap[node.Index];
                    node.PreviousNodeIndex = -1;
                    nodeArray[node.Index] = node;
                }
            }

            // Init start node
            var startNode = nodeArray[GetIndex(From.x, From.y, GridSize.x)];
            startNode.GCost = 0;
            startNode.CalculateFCost();
            nodeArray[startNode.Index] = startNode;

            var endNodeIndex = GetIndex(To.x, To.y, GridSize.x);

            NativeList<int> openList = new NativeList<int>(Allocator.Temp);
            NativeList<int> closedList = new NativeList<int>(Allocator.Temp);

            NativeArray<int2> neighbourOffset = new NativeArray<int2>(8, Allocator.Temp);
            neighbourOffset[0] = new int2(0, -1);
            neighbourOffset[1] = new int2(1, -1);
            neighbourOffset[2] = new int2(1, 0);
            neighbourOffset[3] = new int2(1, 1);
            neighbourOffset[4] = new int2(0, 1);
            neighbourOffset[5] = new int2(-1, 1);
            neighbourOffset[6] = new int2(-1, 0);
            neighbourOffset[7] = new int2(-1, -1);

            openList.Add(startNode.Index);

            while (openList.Length > 0)
            {
                var currentNodeIndex = GetLowestCostFNodeIndex(openList, nodeArray);
                var currentNode = nodeArray[currentNodeIndex];
                if (currentNodeIndex == endNodeIndex)
                {
                    break;
                }

                for (var i = 0; i < openList.Length; i++)
                {
                    if (openList[i] == currentNodeIndex)
                    {
                        openList.RemoveAtSwapBack(i);
                        break;
                    }
                }

                closedList.Add(currentNodeIndex);

                for (var i = 0; i < neighbourOffset.Length; i++)
                {
                    var offset = neighbourOffset[i];
                    var neighbourPosition = new int2(currentNode.X + offset.x, currentNode.Y + offset.y);

                    if (!IsPositionInsideGrid(neighbourPosition, GridSize))
                    {
                        continue;
                    }

                    var neighbourNodeIndex = GetIndex(neighbourPosition.x, neighbourPosition.y, GridSize.x);

                    if (closedList.Contains(neighbourNodeIndex))
                    {
                        continue;
                    }

                    var neighbourNode = nodeArray[neighbourNodeIndex];
                    if (!neighbourNode.IsWalkable)
                    {
                        continue;
                    }

                    var currentNodePosition = new int2(currentNode.X, currentNode.Y);

                    var tentativeGCost = currentNode.GCost + GetDistanceCost(currentNodePosition, neighbourPosition);
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
                for (var i = pathList.Length - 1; i >= 0; i--)
                {
                    PathResult.Add(new int2(pathList[i].x, pathList[i].y));
                }
                pathList.Dispose();
            }

            nodeArray.Dispose();
            openList.Dispose();
            closedList.Dispose();
            neighbourOffset.Dispose();
        }

        private int GetIndex(int x, int y, int gridWidth)
        {
            return x + y * gridWidth;
        }

        private int GetDistanceCost(int2 from, int2 to)
        {
            int xDistance = Mathf.Abs(from.x - to.x);
            int yDistance = Mathf.Abs(from.y - to.y);
            int remaining = Mathf.Abs(xDistance - yDistance);
            return MOVE_COST_DIAGONAL * Mathf.Min(xDistance, yDistance) + MOVE_COST_STRAIGHT * remaining;
        }

        private int GetLowestCostFNodeIndex(NativeList<int> openList, NativeArray<Node> nodeArray)
        {
            var lowestCostNode = nodeArray[openList[0]];
            for (var i = 0; i < openList.Length; i++)
            {
                var currentNode = nodeArray[openList[i]];
                if (currentNode.FCost < lowestCostNode.FCost)
                {
                    lowestCostNode = currentNode;
                }
            }

            return lowestCostNode.Index;
        }

        private bool IsPositionInsideGrid(int2 position, int2 gridSize)
        {
            return position.x >= 0 &&
                   position.y >= 0 &&
                   position.x < gridSize.x &&
                   position.y < gridSize.y;
        }

        private NativeList<int2> GetPath(NativeArray<Node> nodeArray, Node endNode)
        {
            if (endNode.PreviousNodeIndex == -1)
            {
                return new NativeList<int2>(Allocator.Temp);
            }
            else
            {
                NativeList<int2> path = new NativeList<int2>(Allocator.Temp);
                path.Add(new int2(endNode.X, endNode.Y));

                var currentNode = endNode;
                while (currentNode.PreviousNodeIndex != -1)
                {
                    var previousNode = nodeArray[currentNode.PreviousNodeIndex];
                    path.Add(new int2(previousNode.X, previousNode.Y));
                    currentNode = previousNode;
                }

                return path;
            }
        }
    }
}

#endif
