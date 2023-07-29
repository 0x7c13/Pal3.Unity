// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Core.DataReader.Mv3
{
    using GameBox;
    using UnityEngine;

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

    public struct Mv3Mesh
    {
        public string Name; // 64 chars max
        public Bounds Bounds;
        public Mv3Attribute[] Attributes;
        public Vector3[] Normals;
        public int[] Triangles;
        public Vector2[] Uvs;
        public Mv3AnimationKeyFrame[] KeyFrames;
    }

    public struct Mv3AnimationKeyFrame
    {
        public uint Tick;
        public Vector3[] Vertices;
    }

    /// <summary>
    /// MV3 (.mv3) file model
    /// </summary>
    public sealed class Mv3File
    {
        public uint Duration { get; }
        public Mv3AnimationEvent[] AnimationEvents { get; }
        public Mv3TagNode[] TagNodes { get; }
        public Mv3Mesh[] Meshes { get; }
        public GameBoxMaterial[] Materials { get; }

        public Mv3File(uint duration,
            Mv3AnimationEvent[] animationEvents,
            Mv3TagNode[] tagNodes,
            Mv3Mesh[] meshes,
            GameBoxMaterial[] materials)
        {
            Duration = duration;
            AnimationEvents = animationEvents;
            TagNodes = tagNodes;
            Meshes = meshes;
            Materials = materials;
        }
    }
}
