// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2025, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Core.DataReader.Nav
{
    using Contract.Enums;
    using Primitives;

    // NAV (.nav) file header
    internal struct NavHeader
    {
        public string Label; // 4 char
        public byte Version;
        public byte NumberOfLayers;
        public uint TileOffset;
        public uint FaceOffset;
    }

    public struct NavTile
    {
        public float GameBoxYPosition;           // 格子地板高度（原GameBox引擎下的Y坐标单位）
        public byte DistanceToNearestObstacle;   // 格子距离周边障碍物的距离 (0最小-7最大)
        public FloorType FloorType;              // 格子地板类型：0~128索引普通地面属性(土草雪等)，128~255索引机关序号

        public bool IsObstacle;                  // 是否为障碍物

        public bool IsWalkable()
        {
            return !IsObstacle && DistanceToNearestObstacle > 0 && FloorType != FloorType.Jumpable;
        }
    }

    public struct NavLayer
    {
        public GameBoxRect[] Portals; // Max of 8
        public GameBoxVector3 GameBoxMaxWorldPosition;
        public GameBoxVector3 GameBoxMinWorldPosition;
        public int Width;
        public int Height;
        public NavTile[] Tiles;
    }

    public struct NavMeshData
    {
        public GameBoxVector3[] GameBoxVertices;
        public int[] GameBoxTriangles;
        //public NavLayerNode[] Nodes;
    }

    /// <summary>
    /// NAV (.nav) file model
    /// </summary>
    public sealed class NavFile
    {
        public NavLayer[] Layers { get; }
        public NavMeshData[] MeshData { get; }

        public NavFile(NavLayer[] layers, NavMeshData[] meshData)
        {
            Layers = layers;
            MeshData = meshData;
        }
    }
}