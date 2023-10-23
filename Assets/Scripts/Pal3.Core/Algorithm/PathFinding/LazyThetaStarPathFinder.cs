// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Core.Algorithm.PathFinding
{
    using System;
    using System.Collections.Generic;
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
        public static IList<(int x, int y)> FindPath(
            (int x, int y) from,
            (int x, int y) to,
            (int width, int height) gridSize,
            Func<(int x, int y), bool> isObstacle,
            Func<(int x, int y), (int x, int y), int> heuristicFunc)
        {
            var nodes = new SearchNode[gridSize.width, gridSize.height];

            // Init
            for (var x = 0; x < gridSize.width; x++)
            {
                for (var y = 0; y < gridSize.height; y++)
                {
                    (int x, int y) position = (x, y);
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

        private static IList<(int x, int y)> SearchPath(SearchNode startNode,
            SearchNode endNode,
            SearchNode[,] nodes,
            Func<(int x, int y), (int x, int y), int> heuristicFunc)
        {
            IPriorityQueue<(int x, int y), float> searchQueue = new SimplePriorityQueue<(int x, int y)>();

            AddNodeToQueue(startNode, searchQueue);
            while (searchQueue.Count > 0)
            {
                (int x, int y) currentPosition = searchQueue.Dequeue();
                SearchNode node = GetNode(currentPosition.x, currentPosition.y, nodes);

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
                                             heuristicFunc((neighbor.X, neighbor.Y), (node.X, node.Y));
                                if (newGCost < node.GCost)
                                    node.SetParent(neighbor, newGCost);
                            }
                        }
                    }
                }

                if (currentPosition.x == endNode.X &&
                    currentPosition.y == endNode.Y) break;

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

            var path = new List<(int x, int y)>();
            SearchNode lastNode = GetNode(endNode.X, endNode.Y, nodes);
            while (lastNode != null)
            {
                path.Add((lastNode.X, lastNode.Y));
                lastNode.SearchType = SearchType.Path;
                lastNode = lastNode.Parent;
            }

            path.Reverse();
            return path;
        }

        private static bool IsPositionInsideGrid(int x, int y, SearchNode[,] nodes)
        {
            return (x >= 0 && x < nodes.GetLength(0) && y >= 0 && y < nodes.GetLength(1));
        }

        private static SearchNode GetNode(int x, int y, SearchNode[,] nodes)
        {
            return IsPositionInsideGrid(x, y, nodes) ? nodes[x, y] : null;
        }

        private static bool IsNeighborValid(SearchNode neighbor)
        {
            return neighbor.Closed == false;
        }

        private static void AddNodeToQueue(SearchNode node, IPriorityQueue<(int x, int y), float> queue)
        {
            queue.Enqueue((node.X, node.Y), node.CalculateFCost());
            node.Opened = true;
            node.SearchType = SearchType.Open;
        }

        private static void ComputeCost(SearchNode currentNode,
            SearchNode nextNode,
            SearchNode startNode,
            Func<(int x, int y), (int x, int y), int> heuristicFunc)
        {
            SearchNode parent = (currentNode.X == startNode.X && currentNode.Y == startNode.Y)  ?
                startNode : currentNode.Parent;

            float gCost = parent.GCost + heuristicFunc((parent.X, parent.Y), (nextNode.X, nextNode.Y));
            if (gCost < nextNode.GCost)
            {
                nextNode.SetParent(parent, gCost);
            }
        }

        private static List<SearchNode> GetNeighbors(SearchNode node, SearchNode[,] nodes)
        {
            List<SearchNode> result = new List<SearchNode>();
            (int x, int y) position = (node.X, node.Y);

            bool left = TryAddNode(position, -1, 0, result, nodes);
            bool right = TryAddNode(position, 1, 0, result, nodes);
            bool top = TryAddNode(position, 0, 1, result, nodes);
            bool bottom = TryAddNode(position, 0, -1, result, nodes);

            if (left || top) TryAddNode(position, -1, 1, result, nodes);
            if (left || bottom) TryAddNode(position, -1, -1, result, nodes);
            if (right || bottom) TryAddNode(position, 1, -1, result, nodes);
            if (right || top) TryAddNode(position, 1, 1, result, nodes);

            return result;
        }

        private static bool TryAddNode((int x, int y) curtPosition,
            int dx,
            int dy,
            List<SearchNode> result,
            SearchNode[,] nodes)
        {
            SearchNode node = GetNode(curtPosition.x + dx, curtPosition.y + dy, nodes);

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

            int dx = Math.Abs(x1 - x0);
            int dy = Math.Abs(y1 - y0);
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