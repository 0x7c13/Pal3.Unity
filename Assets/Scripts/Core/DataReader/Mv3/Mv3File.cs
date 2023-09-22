// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Core.DataReader.Mv3
{
    using Primitives;

    public struct Mv3AnimationEvent
    {
        public uint GameBoxTick;
        public string Name; // 16 chars max
    }

    public struct Mv3TagFrame
    {
        public uint GameBoxTick;
        public GameBoxVector3 GameBoxPosition;
        public GameBoxQuaternion GameBoxRotation;
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
        public uint GameBoxTick;
        public Mv3Vert[] Vertices;
    }

    public struct Mv3Mesh
    {
        public string Name; // 64 chars max
        public GameBoxVector3 GameBoxBoundsMin;
        public GameBoxVector3 GameBoxBoundsMax;
        public Mv3Attribute[] Attributes;
        public GameBoxVector3[] GameBoxNormals;
        public int[] GameBoxTriangles;
        public GameBoxVector2[] Uvs;
        public Mv3AnimationKeyFrame[] KeyFrames;
    }

    public struct Mv3AnimationKeyFrame
    {
        public uint GameBoxTick;
        public GameBoxVector3[] GameBoxVertices;
    }

    /// <summary>
    /// MV3 (.mv3) file model
    /// </summary>
    public sealed class Mv3File
    {
        public uint TotalGameBoxTicks { get; }
        public Mv3AnimationEvent[] AnimationEvents { get; }
        public Mv3TagNode[] TagNodes { get; }
        public Mv3Mesh[] Meshes { get; }
        public GameBoxMaterial[] Materials { get; }

        public Mv3File(uint totalGameBoxTicks,
            Mv3AnimationEvent[] animationEvents,
            Mv3TagNode[] tagNodes,
            Mv3Mesh[] meshes,
            GameBoxMaterial[] materials)
        {
            TotalGameBoxTicks = totalGameBoxTicks;
            AnimationEvents = animationEvents;
            TagNodes = tagNodes;
            Meshes = meshes;
            Materials = materials;
        }
    }
}
