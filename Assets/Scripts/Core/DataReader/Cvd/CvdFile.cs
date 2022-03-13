﻿// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Core.DataReader.Cvd
{
    using GameBox;
    using UnityEngine;

    public enum CvdAnimationKeyType
    {
        Tcb     = 1,
        Bezier  = 2,
        Linear  = 3
    }

    public struct CvdAnimationKey
    {
        public float Time;
        public uint Flags;
    }

    public struct CvdTcbKey
    {
        public CvdAnimationKey AnimationKey;
        public float Tension;
        public float Continuity;
        public float Bias;
        public float EaseIn;
        public float EaseOut;
    }

    public struct CvdTcbVector3Key
    {
        public CvdTcbKey TcbKey;
        public Vector3 Value;
    }

    public struct CvdTcbRotationKey
    {
        public CvdTcbKey TcbKey;
        public Vector3 Axis;
        public float Angle;
    }

    public struct CvdTcbFloatKey
    {
        public CvdTcbKey TcbKey;
        public float Value;
    }

    public struct CvdTcbScaleKey
    {
        public CvdTcbKey TcbKey;
        public Vector3 Value;
        public GameBoxQuaternion Rotation;
    }

    public struct CvdBezierVector3Key
    {
        public CvdAnimationKey AnimationKey;
        public Vector3 TangentIn;
        public Vector3 TangentOut;
        public Vector3 Value;
    }

    public struct CvdBezierRotationKey
    {
        public CvdAnimationKey AnimationKey;
        public GameBoxQuaternion Value;
    }

    public struct CvdBezierFloatKey
    {
        public CvdAnimationKey AnimationKey;
        public float TangentIn;
        public float TangentOut;
        public float Value;
        public float LengthIn;
        public float LengthOut;
    }

    public struct CvdBezierScaleKey
    {
        public CvdAnimationKey AnimationKey;
        public Vector3 TangentIn;
        public Vector3 TangentOut;
        public Vector3 Value;
        public GameBoxQuaternion Rotation;
    }

    public struct CvdLinearVector3Key
    {
        public CvdAnimationKey AnimationKey;
        public Vector3 Value;
    }

    public struct CvdLinearFloatKey
    {
        public CvdAnimationKey AnimationKey;
        public float Value;
    }

    public struct CvdLinearRotationKey
    {
        public CvdAnimationKey AnimationKey;
        public GameBoxQuaternion Value;
    }

    public struct CvdLinearScaleKey
    {
        public CvdAnimationKey AnimationKey;
        public Vector3 Value;
        public GameBoxQuaternion Rotation;
    }

    // CVD (.cvd) file header
    public struct CvdHeader
    {
        public string Magic; // 4 chars
        public int Version;
        public int NumberOfRootNodes;
    }

    public struct CvdGeometryNode
    {
        public CvdAnimationPositionKeyFrame[] PositionKeyInfos;
        public CvdAnimationRotationKeyFrame[] RotationKeyInfos;
        public CvdAnimationScaleKeyFrame[] ScaleKeyInfos;
        public float Scale;
        public GameBoxMatrix4X4 TransformMatrix;
        public CvdGeometryNode[] Children;
        public CvdMesh Mesh;
    }

    public struct CvdVertex
    {
        public Vector3 Position;
        public Vector3 Normal;
        public Vector2 Uv;
    }

    public struct CvdMesh
    {
        public float[] AnimationTimeKeys;
        public CvdMeshSection[] MeshSections;
    }

    public struct CvdMeshSection
    {
        public CvdVertex[][] FrameVertices;
        public int[] Triangles;
        public byte BlendFlag;
        public GameBoxMaterial Material;
        public string TextureName;
        public float[] AnimationTimeKeys;
        public GameBoxMaterial[] AnimationMaterials;
    }

    public abstract class CvdAnimationKeyFrame
    {
        public CvdAnimationKeyType KeyType { get; set; }
        public float Time { get; set; }
    }

    public class CvdAnimationPositionKeyFrame : CvdAnimationKeyFrame
    {
        public Vector3 Position { get; set; }
    }

    public class CvdAnimationScaleKeyFrame : CvdAnimationKeyFrame
    {
        public Vector3 Scale { get; set; }
        public GameBoxQuaternion Rotation { get; set; }
    }

    public class CvdAnimationRotationKeyFrame : CvdAnimationKeyFrame
    {
        public GameBoxQuaternion Rotation { get; set; }
    }

    public class CvdFile
    {
        public float AnimationDuration { get; }
        public CvdGeometryNode[] RootNodes { get; }

        public CvdFile(float animationDuration, CvdGeometryNode[] rootNodes)
        {
            AnimationDuration = animationDuration;
            RootNodes = rootNodes;
        }
    }
}