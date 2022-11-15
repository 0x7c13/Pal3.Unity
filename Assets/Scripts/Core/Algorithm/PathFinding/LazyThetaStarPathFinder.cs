// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Core.Algorithm.PathFinding
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;
    using PriorityQueue;

    public enum SearchType
    {
        None,
        Start,
        Goal,
        Open,
        Expanded,
        Path
    }

    internal class SearchNode
    {
        public int X { get; set; }

        public int Y { get; set; }

        public Vector2Int Position => new (X, Y);

        public bool IsObstacle { get; set; }

        public SearchType SearchType { get; set; }

        public SearchNode Parent { get; private set; }

        private bool _opened;
        public bool Opened
        {
            get => _opened;
            set
            {
                _opened = value;
                if (_opened)
                {
                    _closed = false;
                }
            }
        }

        private bool _closed;
        public bool Closed
        {
            get => _closed;
            set
            {
                _closed = value;
                if (_closed)
                {
                    _opened = false;
                }
            }
        }

        public float GCost { get; set; } = float.MaxValue;

        public float HCost { get; set; }

        public void SetParent(SearchNode parent, float gCost)
        {
            Parent = parent;
            GCost = gCost;
        }

        // GCost + HCost
        public float CalculateFCost()
        {
            return GCost + HCost;
        }
    }

    /// <summary>
    /// Lazy theta* path finding algorithm.
    /// </summary>
    public static class LazyThetaStarPathFinder
    {
        public static IList<Vector2Int> FindPath(
            Vector2Int from,
            Vector2Int to,
            Vector2Int gridSize,
            Func<Vector2Int, bool> isObstacle,
            Func<Vector2Int, Vector2Int, int> heuristicFunc)
        {
            var nodes = new SearchNode[gridSize.x, gridSize.y];

            // Init
            for (var x = 0; x < gridSize.x; x++)
            {
                for (var y = 0; y < gridSize.y; y++)
                {
                    var position = new Vector2Int(x, y);
                    var node = new SearchNode()
                    {
                        X = x,
                        Y = y,
                        IsObstacle = isObstacle(position),
                        HCost = heuristicFunc(position, to)
                    };

                    nodes[x, y] = node;
                }
            }

            SearchNode startNode = nodes[from.x, from.y];
            startNode.IsObstacle = false;
            startNode.SearchType = SearchType.Start;
            startNode.GCost = 0;

            SearchNode endNode = nodes[to.x, to.y];
            endNode.IsObstacle = false;
            startNode.SearchType = SearchType.Goal;

            return SearchPath(startNode, endNode, nodes, heuristicFunc);
        }

        private static IList<Vector2Int> SearchPath(SearchNode startNode,
            SearchNode endNode,
            SearchNode[,] nodes,
            Func<Vector2Int, Vector2Int, int> heuristicFunc)
        {
            IPriorityQueue<Vector2Int, float> searchQueue = new SimplePriorityQueue<Vector2Int>();

            AddNodeToQueue(startNode, searchQueue);
            while (searchQueue.Count > 0)
            {
                Vector2Int currentPosition = searchQueue.Dequeue();
                SearchNode node = GetNode(currentPosition, nodes);

                if (!(node.X == startNode.X && node.Y == startNode.Y))
                {
                    if (!LineOfSight(node.Parent, node, nodes))
                    {
                        node.SetParent(null, float.MaxValue);

                        var currentNeighbors = GetNeighbors(node, nodes);
                        for (var i = 0; i < currentNeighbors.Count; i++)
                        {
                            SearchNode neighbor = currentNeighbors[i];
                            if (neighbor.Closed)
                            {
                                var newGCost = neighbor.GCost +
                                             heuristicFunc(new Vector2Int(neighbor.X, neighbor.Y), new Vector2Int(node.X, node.Y));
                                if (newGCost < node.GCost)
                                    node.SetParent(neighbor, newGCost);
                            }
                        }
                    }
                }

                if (currentPosition == endNode.Position) break;

                node.SearchType = SearchType.Expanded;
                node.Closed = true;

                var neighbors = GetNeighbors(node, nodes);
                for (var i = 0; i < neighbors.Count; i++)
                {
                    SearchNode neighbor = neighbors[i];
                    if (IsNeighborValid(neighbor))
                    {
                        if (neighbor.Opened == false)
                            neighbor.SetParent(null, float.MaxValue);

                        float oldGCost = neighbor.GCost;
                        ComputeCost(node, neighbor, startNode, heuristicFunc);
                        if (neighbor.GCost < oldGCost)
                        {
                            if (neighbor.Opened == false)
                            {
                                AddNodeToQueue(neighbor, searchQueue);
                            }
                        }
                    }
                }
            }

            var path = new List<Vector2Int>();
            SearchNode lastNode = GetNode(endNode.Position, nodes);
            while (lastNode != null)
            {
                path.Add(new Vector2Int(lastNode.X, lastNode.Y));
                lastNode.SearchType = SearchType.Path;
                lastNode = lastNode.Parent;
            }

            path.Reverse();
            return path.ToArray();
        }

        private static bool IsPositionInsideGrid(int x, int y, SearchNode[,] nodes)
        {
            return (x >= 0 && x < nodes.GetLength(0) && y >= 0 && y < nodes.GetLength(1));
        }

        private static SearchNode GetNode(Vector2Int pos, SearchNode[,] nodes)
        {
            return GetNode(pos.x, pos.y, nodes);
        }

        private static SearchNode GetNode(int x, int y, SearchNode[,] nodes)
        {
            return IsPositionInsideGrid(x, y, nodes) ? nodes[x, y] : null;
        }

        private static bool IsNeighborValid(SearchNode neighbor)
        {
            return neighbor.Closed == false;
        }

        private static void AddNodeToQueue(SearchNode node, IPriorityQueue<Vector2Int, float> queue)
        {
            queue.Enqueue(node.Position, node.CalculateFCost());
            node.Opened = true;
            node.SearchType = SearchType.Open;
        }

        private static void ComputeCost(SearchNode currentNode,
            SearchNode nextNode,
            SearchNode startNode,
            Func<Vector2Int, Vector2Int, int> heuristicFunc)
        {
            SearchNode parent = (currentNode.X == startNode.X && currentNode.Y == startNode.Y)  ?
                startNode : currentNode.Parent;

            float gCost = parent.GCost +
                         heuristicFunc(new Vector2Int(parent.X, parent.Y), new Vector2Int(nextNode.X, nextNode.Y));
            if (gCost < nextNode.GCost)
            {
                nextNode.SetParent(parent, gCost);
            }
        }

        private static List<SearchNode> GetNeighbors(SearchNode node, SearchNode[,] nodes)
        {
            List<SearchNode> result = new List<SearchNode>();
            Vector2Int pos = node.Position;

            bool left = TryAddNode(pos, -1, 0, result, nodes);
            bool right = TryAddNode(pos, 1, 0, result, nodes);
            bool top = TryAddNode(pos, 0, 1, result, nodes);
            bool bottom = TryAddNode(pos, 0, -1, result, nodes);

            if (left || top) TryAddNode(pos, -1, 1, result, nodes);
            if (left || bottom) TryAddNode(pos, -1, -1, result, nodes);
            if (right || bottom) TryAddNode(pos, 1, -1, result, nodes);
            if (right || top) TryAddNode(pos, 1, 1, result, nodes);

            return result;
        }

        private static bool TryAddNode(Vector2Int curtPos,
            int dx,
            int dy,
            List<SearchNode> result,
            SearchNode[,] nodes)
        {
            SearchNode node = GetNode(curtPos.x + dx, curtPos.y + dy, nodes);

            if (node is {IsObstacle: false})
            {
                result.Add(node);
                return true;
            }

            return false;
        }

        private static bool LineOfSight(SearchNode start, SearchNode end, SearchNode[,] nodes)
        {
            int x0 = start.X;
            int y0 = start.Y;
            int x1 = end.X;
            int y1 = end.Y;

            int dx = Mathf.Abs(x1 - x0);
            int dy = Mathf.Abs(y1 - y0);
            int sx = x0 < x1 ? 1 : -1;
            int sy = y0 < y1 ? 1 : -1;
            int err = dx - dy;

            while (true)
            {
                if (x0 == x1 && y0 == y1)
                {
                    return true;
                }

                SearchNode node = GetNode(x0, y0, nodes);
                if (node is {IsObstacle: true})
                {
                    return false;
                }

                int e2 = 2 * err;
                if (e2 > -dy)
                {
                    err -= dy;
                    x0 += sx;
                }

                if (e2 < dx)
                {
                    err += dx;
                    y0 += sy;
                }
            }
        }
    }
}