// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
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

    public struct PolVertex
    {
        public Vector3 Position;
        public Vector3 Normal;
        public float Radius;
        public Color32 DiffuseColor;
        public Color32 SpecularColor;
        public Vector2[] Uv;
    }

    public struct PolMesh
    {
        public GameBoxAABBox BoundBox;
        public uint VertexFvfFlag;
        public PolVertex[] Vertices;
        public PolTexture[] Textures;
    }

    public struct PolTexture
    {
        public uint BlendFlag; // 0--opaque, 1--alpha blend, 2--invert color blend, 3--add
        public GameBoxMaterial Material;
        public string[] TextureNames;
        public int VertStart;
        public int VertEnd;
        public (short x, short y, short z)[] Triangles;
    }

    /// <summary>
    /// POLY (.pol) file model
    /// </summary>
    public class PolFile
    {
        public int Version { get; }
        public PolGeometryNode[] NodeDescriptions { get; }
        public PolMesh[] Meshes { get; }

        public PolFile(int version,
            PolGeometryNode[] nodeDescriptions,
            PolMesh[] meshes)
        {
            Version = version;
            NodeDescriptions = nodeDescriptions;
            Meshes = meshes;
        }
    }
}