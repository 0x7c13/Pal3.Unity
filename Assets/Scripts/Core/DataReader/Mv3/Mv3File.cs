// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Core.DataReader.Mv3
{
    using GameBox;
    using UnityEngine;

    // MV3 (.mv3) file header
    internal struct Mv3Header
    {
        public string Magic; // 4 char
        public int Version;
        public uint Duration;
        public int NumberOfMaterials;
        public int NumberOfTagNodes;
        public int NumberOfMeshes;
        public int NumberOfAnimationEvents;
    }

    public struct Mv3AnimationEvent
    {
        public uint Tick;
        public string Name; // 16 chars max
    }

    public struct Mv3TagFrame
    {
        public uint Tick;
        public Vector3 Position;
        public Quaternion Rotation;
        public float[][] Scale; // 3x3
    }

    public struct Mv3TagNode
    {
        public string Name; // 64 chars max
        public Mv3TagFrame[] TagFrames;
        public float FlipScale;
    }

    public struct Mv3IndexBuffer
    {
        public ushort[] TriangleIndex; // 3
        public ushort[] TexCoordIndex; // 3
    }

    public struct Mv3Attribute
    {
        public int MaterialId;
        public Mv3IndexBuffer[] IndexBuffers;
        public int[] Commands;
    }

    public struct Mv3Vert
    {
        public short X;
        public short Y;
        public short Z;
        public ushort N;
    }

    public struct Mv3VertFrame
    {
        public uint Tick;
        public Mv3Vert[] Vertices;
    }

    public struct Mv3Material
    {
        public GameBoxMaterial Material;
        public string[] TextureNames;
    }

    public struct Mv3Mesh
    {
        public string Name; // 64 chars max
        public GameBoxAABBox BoundBox;
        public Mv3Attribute[] Attributes;
        public Mv3VertFrame[] Frames;
        public Vector2[] TexCoords;
    }

    public struct VertexAnimationKeyFrame
    {
        public uint Tick;
        public Vector3[] Vertices;
        public Vector3[] Normals;
        public int[] Triangles;
        public Vector2[] Uv;
    }

    /// <summary>
    /// MV3 (.mv3) file model
    /// </summary>
    public class Mv3File
    {
        public int Version { get; }
        public uint Duration { get; }
        public Mv3AnimationEvent[] AnimationEvents { get; }
        public Mv3TagNode[] TagNodes { get; }
        public Mv3Material[] Materials { get; }
        public Mv3Mesh[] Meshes { get; }
        public VertexAnimationKeyFrame[][] MeshKeyFrames { get; }

        public Mv3File(int version,
            uint duration,
            Mv3AnimationEvent[] animationEvents,
            Mv3TagNode[] tagNodes,
            Mv3Material[] materials,
            Mv3Mesh[] meshes,
            VertexAnimationKeyFrame[][] meshKeyFrames)
        {
            Version = version;
            Duration = duration;
            AnimationEvents = animationEvents;
            TagNodes = tagNodes;
            Materials = materials;
            Meshes = meshes;
            MeshKeyFrames = meshKeyFrames;
        }
    }
}
