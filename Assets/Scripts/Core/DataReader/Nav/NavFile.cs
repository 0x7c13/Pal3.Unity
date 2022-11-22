// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Core.DataReader.Nav
{
    using GameBox;
    using UnityEngine;

    // NAV (.nav) file header
    internal struct NavHeader
    {
        public string Label; // 4 char
        public byte Version;
        public byte NumberOfLayers;
        public uint TileOffset;
        public uint FaceOffset;
    }

    public enum NavFloorKind
    {
        Default = 0,      // 不可跳
        Jumpable,         // 可跳过
        Soil,             // 土
        Mud,              // 泥
        River,            // 水道(船状态)
        Snow,             // 雪
        Wet,              // 水浸
        Grass,            // 草
        Wood,             // 竹木
        Plant,            // 植物
        Wetland,          // 沼泽
        Sand,             // 沙
        Metal,            // 金属
        Rock1,            // 熔岩
        Rock2,            // 岩石基底地面
        Rock3,            // 人工石地面
        Brick,            // 砖
        Unknown1,         // 异空间A
        Unknown2,         // 异空间B
        Unknown3,         // 异空间C
        Other,            // 其它
        FloorIce,         // 冰
        FloorBlood,       // 血池
        FloorWater,       // 水域
    }

    public struct NavTile
    {
        public float GameBoxYPosition;           // 格子地板高度（原GameBox引擎下的Y坐标单位）
        public byte DistanceToNearestObstacle;   // 格子距离周边障碍物的距离 (0最小-7最大)
        public NavFloorKind FloorKind;           // 格子地板类型：0~128索引普通地面属性(土草雪等)，128~255索引机关序号

        public bool IsWalkable()
        {
            return DistanceToNearestObstacle > 0 || FloorKind == NavFloorKind.Jumpable; // TODO: Remove Jumpable 
        }
    }

    // public struct NavLayerNode
    // {
    //     public GameBoxAABBox BoundBox;
    //     public Vector3[] Triangles;
    //     public int Level; // 0: root,  child level = parent level + 1
    //     public NavLayerNode[] Children; // 4
    // }

    public struct NavTileLayer
    {
        public GameBoxRect[] Portals; // Max of 8
        public Vector3 Max;
        public Vector3 Min;
        public int Width;
        public int Height;
        public NavTile[] Tiles;
    }

    public struct NavFaceLayer
    {
        public Vector3[] Vertices;
        public int[] Triangles;
        //public NavLayerNode[] Nodes;
    }

    /// <summary>
    /// NAV (.nav) file model
    /// </summary>
    public class NavFile
    {
        public NavTileLayer[] TileLayers { get; }
        public NavFaceLayer[] FaceLayers { get; }

        public NavFile(NavTileLayer[] tileLayers, NavFaceLayer[] faceLayers)
        {
            TileLayers = tileLayers;
            FaceLayers = faceLayers;
        }
    }
}