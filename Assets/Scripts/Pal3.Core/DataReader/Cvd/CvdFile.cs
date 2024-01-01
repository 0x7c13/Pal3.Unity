// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2024, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Core.DataReader.Cvd
{
    using Primitives;

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
        public GameBoxVector3 Value;
    }

    public struct CvdTcbRotationKey
    {
        public CvdTcbKey TcbKey;
        public GameBoxVector3 Axis;
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
        public GameBoxVector3 Value;
        public GameBoxQuaternion Rotation;
    }

    public struct CvdBezierVector3Key
    {
        public CvdAnimationKey AnimationKey;
        public GameBoxVector3 TangentIn;
        public GameBoxVector3 TangentOut;
        public GameBoxVector3 Value;
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
        public GameBoxVector3 TangentIn;
        public GameBoxVector3 TangentOut;
        public GameBoxVector3 Value;
        public GameBoxQuaternion Rotation;
    }

    public struct CvdLinearVector3Key
    {
        public CvdAnimationKey AnimationKey;
        public GameBoxVector3 Value;
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
        public GameBoxVector3 Value;
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
        public GameBoxMatrix4x4 TransformMatrix;
        public CvdGeometryNode[] Children;
        public CvdMesh Mesh;
        public bool IsGeometryNode;
    }

    public struct CvdVertex
    {
        public GameBoxVector3 GameBoxPosition;
        public GameBoxVector3 GameBoxNormal;
        public GameBoxVector2 Uv;
    }

    public struct CvdMesh
    {
        public float[] AnimationTimeKeys;
        public CvdMeshSection[] MeshSections;
    }

    public struct CvdMeshSection
    {
        public CvdVertex[][] FrameVertices;
        public int[] GameBoxTriangles;
        public GameBoxBlendFlag BlendFlag;
        public GameBoxMaterial Material;
        public float[] AnimationTimeKeys;
        public GameBoxMaterial[] AnimationMaterials;
    }

    public abstract class CvdAnimationKeyFrame
    {
        public CvdAnimationKeyType KeyType { get; set; }
        public float Time { get; set; }
    }

    public sealed class CvdAnimationPositionKeyFrame : CvdAnimationKeyFrame
    {
        public GameBoxVector3 GameBoxPosition { get; set; }
    }

    public sealed class CvdAnimationScaleKeyFrame : CvdAnimationKeyFrame
    {
        public GameBoxVector3 GameBoxScale { get; set; }
        public GameBoxQuaternion GameBoxRotation { get; set; }
    }

    public sealed class CvdAnimationRotationKeyFrame : CvdAnimationKeyFrame
    {
        public GameBoxQuaternion GameBoxRotation { get; set; }
    }

    /// <summary>
    /// CVD (.cvd) file model
    /// </summary>
    public sealed class CvdFile
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