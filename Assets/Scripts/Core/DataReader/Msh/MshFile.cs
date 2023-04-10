// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Core.DataReader.Msh
{
    using System;
    using UnityEngine;

    public enum BoneNodeType
    {
        Normal = 0,
        Bone,
        Dummy,
    }

    [Flags]
    public enum MshLayerFlag
    {
        Position = 1 << 0,
        Normal =   1 << 1,
        Tex1 =     1 << 2,
        Tex2 =     1 << 3,
        Tex3 =     1 << 4,
        Tex4 =     1 << 5,
        Tex5 =     1 << 6,
        Tex6 =     1 << 7,
        Tex7 =     1 << 8,
        Tex8 =     1 << 9,
        Lod =      1 << 10,
        Bone =     1 << 11,
        Physics =  1 << 12,
    }

    public struct PhyVertex
    {
        public Vector3 Position;
        public byte[] BoneIds;
        public byte[] Weights;
    }

    public struct PhyFace
    {
        public int[] Indices;
        public ushort[] Normals;
        public float[,] U;
        public float[,] V;
    }

    public struct MshMesh
    {
        public int MaterialId;
        public MshLayerFlag LayerFlags;
        public PhyVertex[] Vertices;
        public PhyFace[] Faces;
    }

    public class BoneNode
    {
        public BoneNodeType Type;
        public int Id;
        public string Name;
        public BoneNode Parent;
        public int ParentId;
        public BoneNode[] Children;

        public Vector3 Translation;
        public Quaternion Rotation;
        public Matrix4x4 Scale;
        public float FlipScaleSign; // Sign of the determinant of the matrix
        public Matrix4x4 LocalTransform; // The matrix transform from model space to bone space
    }

    public sealed class MshFile
    {
        public BoneNode RootBoneNode { get; }
        public MshMesh[] SubMeshes { get; }

        public MshFile(BoneNode rootBoneNode,
            MshMesh[] subMeshes)
        {
            RootBoneNode = rootBoneNode;
            SubMeshes = subMeshes;
        }
    }
}