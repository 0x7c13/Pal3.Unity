// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Core.DataReader.Pol
{
    using Primitives;

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
        public GameBoxVector3 GameBoxPosition;
        public float Radius;
        public int Offset;
    }

    public struct PolVertexInfo
    {
        public GameBoxVector3[] GameBoxPositions;
        public GameBoxVector3[] GameBoxNormals;
        public Color32[] DiffuseColors;
        public Color32[] SpecularColors;
        public GameBoxVector2[][] Uvs;
    }

    public struct PolMesh
    {
        public GameBoxVector3 GameBoxBoundsMin;
        public GameBoxVector3 GameBoxBoundsMax;
        public uint VertexFvfFlag;
        public PolVertexInfo VertexInfo;
        public PolTexture[] Textures;
    }

    public struct PolTexture
    {
        public GameBoxBlendFlag BlendFlag;
        public GameBoxMaterial Material;
        public uint VertStart;
        public uint VertEnd;
        public int[] GameBoxTriangles;
    }

    public struct TagNode
    {
        public string Name;
        public GameBoxMatrix4x4 GameBoxTransformMatrix;
        public uint TintColor;
    }

    /// <summary>
    /// POLY (.pol) file model
    /// </summary>
    public sealed class PolFile
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