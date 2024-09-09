﻿// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2024, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Core.DataReader.Mov
{
    using System.IO;
    using Primitives;

    public sealed class MovFileReader : IFileReader<MovFile>
    {
        public MovFile Read(IBinaryReader reader, int codepage)
        {
            char[] header = reader.ReadChars(4);
            string headerStr = new string(header[..^1]);

            if (headerStr != "anm")
            {
                throw new InvalidDataException("Invalid MOV(.mov) file: header != anm");
            }

            int version = reader.ReadInt32();
            if (version != 100)
            {
                throw new InvalidDataException("Invalid MOV(.mov) file: version != 100");
            }

            float duration = reader.ReadSingle();
            int numberOfBoneAnimationTracks = reader.ReadInt32();
            int numberOfVertices = reader.ReadInt32();
            int numberOfAnimationEvents = reader.ReadInt32();

            MovAnimationEvent[] animationEvents = new MovAnimationEvent[numberOfAnimationEvents];
            for (int i = 0; i < numberOfAnimationEvents; i++)
            {
                animationEvents[i] = ReadAnimationEvent(reader, codepage);
            }

            MovBoneAnimationTrack[] boneAnimationTracks = new MovBoneAnimationTrack[numberOfBoneAnimationTracks];
            for (int i = 0; i < numberOfBoneAnimationTracks; i++)
            {
                boneAnimationTracks[i] = ReadBoneAnimationTrack(reader, codepage);
            }

            float totalDuration = 0;
            for (int i = 0; i < boneAnimationTracks.Length; i++)
            {
                int numOfKeyFrames = boneAnimationTracks[i].KeyFrames.Length;
                if (numOfKeyFrames > 0)
                {
                    float keySeconds = boneAnimationTracks[i].KeyFrames[numOfKeyFrames - 1].KeySeconds;
                    if (keySeconds > totalDuration)
                    {
                        totalDuration = keySeconds;
                    }
                }
            }

            return new MovFile(totalDuration, boneAnimationTracks, animationEvents);
        }

        private static MovAnimationEvent ReadAnimationEvent(IBinaryReader reader, int codepage)
        {
            return new MovAnimationEvent()
            {
                KeySeconds = reader.ReadSingle(),
                Name = reader.ReadString(16, codepage)
            };
        }

        private static MovBoneAnimationTrack ReadBoneAnimationTrack(IBinaryReader reader, int codepage)
        {
            int boneId = reader.ReadInt32();

            int lengthOfBoneName = reader.ReadInt32();
            string boneName = reader.ReadString(lengthOfBoneName, codepage);

            int numberOfKeyFrames = reader.ReadInt32();
            int animationFlags = reader.ReadInt32();

            MovAnimationKeyFrame[] animationKeyFrames = new MovAnimationKeyFrame[numberOfKeyFrames];
            for (int i = 0; i < animationKeyFrames.Length; i++)
            {
                animationKeyFrames[i] = new MovAnimationKeyFrame()
                {
                    KeySeconds = reader.ReadSingle(),
                    GameBoxTranslation = reader.ReadGameBoxVector3(),
                    GameBoxRotation = new GameBoxQuaternion()
                    {
                        X = reader.ReadSingle(),
                        Y = reader.ReadSingle(),
                        Z = reader.ReadSingle(),
                        W = reader.ReadSingle(),
                    },
                    GameBoxScale = new []
                    {
                        new [] {reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()},
                        new [] {reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()},
                        new [] {reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()},
                    }
                };
            }

            return new MovBoneAnimationTrack()
            {
                BoneId = boneId,
                BoneName = boneName,
                AnimationFlags = animationFlags,
                KeyFrames = animationKeyFrames
            };
        }
    }
}