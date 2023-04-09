// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Core.DataReader.Mov
{
    using UnityEngine;

    public struct MovAnimationKeyFrame
    {
        public uint Tick;
        public Vector3 Translation; // Relative translation to parent bone
        public Quaternion Rotation; // Relative rotation to parent bone
        public float[][] Scale; // 3x3
    }

    public struct MovBoneAnimationTrack
    {
        public int BoneId;
        public string BoneName; // len = 64
        public int AnimationFlags;  // 1 translation, 2 rotation, 4 scale
        public MovAnimationKeyFrame[] KeyFrames;
    }

    public struct MovAnimationEvent
    {
        public uint Tick;
        public string Name; // 16 chars max
    }

    public sealed class MovFile
    {
        public uint Duration { get; }
        public MovBoneAnimationTrack[] BoneAnimationTracks { get; }
        public MovAnimationEvent[] AnimationEvents { get; }

        public MovFile(uint duration,
            MovBoneAnimationTrack[] boneAnimationTracks,
            MovAnimationEvent[] animationEvents)
        {
            Duration = duration;
            BoneAnimationTracks = boneAnimationTracks;
            AnimationEvents = animationEvents;
        }
    }
}