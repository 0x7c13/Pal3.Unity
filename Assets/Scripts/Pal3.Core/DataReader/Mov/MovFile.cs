// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Core.DataReader.Mov
{
    using Primitives;

    public struct MovAnimationKeyFrame
    {
        public float KeySeconds;
        public GameBoxVector3 GameBoxTranslation; // Relative translation to parent bone
        public GameBoxQuaternion GameBoxRotation; // Relative rotation to parent bone
        public float[][] GameBoxScale; // 3x3
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
        public float KeySeconds;
        public string Name; // 16 chars max
    }

    public sealed class MovFile
    {
        public float Duration { get; }
        public MovBoneAnimationTrack[] BoneAnimationTracks { get; }
        public MovAnimationEvent[] AnimationEvents { get; }

        public MovFile(float duration,
            MovBoneAnimationTrack[] boneAnimationTracks,
            MovAnimationEvent[] animationEvents)
        {
            Duration = duration;
            BoneAnimationTracks = boneAnimationTracks;
            AnimationEvents = animationEvents;
        }
    }
}