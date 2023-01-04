// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Core.DataReader.Pol
{
    using GameBox;
    using UnityEngine;

    // POLY (.pol) file header
    public struct PolHeader
    {
        public string Magic; // 4 chars
        public int Version;
        public int NumberOfNodes;
    }

    public struct PolGeometryNode
    {
        public string Name;
        public Vector3 Position;
        public float Radius;
        public int Offset;
    }

    public struct PolVertexInfo
    {
        public Vector3[] Positions;
        public Vector3[] Normals;
        public Color32[] DiffuseColors;
        public Color32[] SpecularColors;
        public Vector2[][] Uvs;
    }

    public struct PolMesh
    {
        public Bounds Bounds;
        public uint VertexFvfFlag;
        public PolVertexInfo VertexInfo;
        public PolTexture[] Textures;
    }

    public struct PolTexture
    {
        public GameBoxBlendFlag BlendFlag;
        public GameBoxMaterial Material;
        public string[] TextureNames;
        public int VertStart;
        public int VertEnd;
        public int[] Triangles;
    }

    public struct TagNode
    {
        public string Name;
        public Vector3 Origin;
        public uint TintColor;
    }

    /// <summary>
    /// POLY (.pol) file model
    /// </summary>
    public class PolFile
    {
        public int Version { get; }
        public PolGeometryNode[] NodeDescriptions { get; }
        public PolMesh[] Meshes { get; }
        public TagNode[] TagNodes { get; }

        public PolFile(int version,
            PolGeometryNode[] nodeDescriptions,
            PolMesh[] meshes,
            TagNode[] tagNodes)
        {
            Version = version;
            NodeDescriptions = nodeDescriptions;
            Meshes = meshes;
            TagNodes = tagNodes;
        }
    }
}