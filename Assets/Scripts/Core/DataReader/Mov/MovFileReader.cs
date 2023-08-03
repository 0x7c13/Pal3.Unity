// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Core.DataReader.Mov
{
    using System.IO;
    using GameBox;

    public sealed class MovFileReader : IFileReader<MovFile>
    {
        private readonly int _codepage;

        public MovFileReader(int codepage)
        {
            _codepage = codepage;
        }

        public MovFile Read(IBinaryReader reader)
        {
            var header = reader.ReadChars(4);
            var headerStr = new string(header[..^1]);

            if (headerStr != "anm")
            {
                throw new InvalidDataException("Invalid MOV(.mov) file: header != anm");
            }

            var version = reader.ReadInt32();
            if (version != 100)
            {
                throw new InvalidDataException("Invalid MOV(.mov) file: version != 100");
            }

            var duration = reader.ReadSingle();
            var numberOfBoneAnimationTracks = reader.ReadInt32();
            var numberOfVertices = reader.ReadInt32();
            var numberOfAnimationEvents = reader.ReadInt32();

            var animationEvents = new MovAnimationEvent[numberOfAnimationEvents];
            for (var i = 0; i < numberOfAnimationEvents; i++)
            {
                animationEvents[i] = ReadAnimationEvent(reader, _codepage);
            }

            var boneAnimationTracks = new MovBoneAnimationTrack[numberOfBoneAnimationTracks];
            for (var i = 0; i < numberOfBoneAnimationTracks; i++)
            {
                boneAnimationTracks[i] = ReadBoneAnimationTrack(reader, _codepage);
            }

            uint totalDuration = 0;
            for (var i = 0; i < boneAnimationTracks.Length; i++)
            {
                int numOfKeyFrames = boneAnimationTracks[i].KeyFrames.Length;
                if (numOfKeyFrames > 0)
                {
                    uint tick = boneAnimationTracks[i].KeyFrames[numOfKeyFrames - 1].Tick;
                    if (tick > totalDuration)
                    {
                        totalDuration = tick;
                    }
                }
            }

            return new MovFile(totalDuration, boneAnimationTracks, animationEvents);
        }

        private static MovAnimationEvent ReadAnimationEvent(IBinaryReader reader, int codepage)
        {
            return new MovAnimationEvent()
            {
                Tick = GameBoxInterpreter.SecondsToTick(reader.ReadSingle()),
                Name = reader.ReadString(16, codepage)
            };
        }

        private static MovBoneAnimationTrack ReadBoneAnimationTrack(IBinaryReader reader, int codepage)
        {
            var boneId = reader.ReadInt32();

            var lengthOfBoneName = reader.ReadInt32();
            var boneName = reader.ReadString(lengthOfBoneName, codepage);

            var numberOfKeyFrames = reader.ReadInt32();
            var animationFlags = reader.ReadInt32();

            var animationKeyFrames = new MovAnimationKeyFrame[numberOfKeyFrames];
            for (var i = 0; i < animationKeyFrames.Length; i++)
            {
                animationKeyFrames[i] = new MovAnimationKeyFrame()
                {
                    Tick = GameBoxInterpreter.SecondsToTick(reader.ReadSingle()),
                    Translation = GameBoxInterpreter.ToUnityPosition(reader.ReadVector3()),
                    Rotation = GameBoxInterpreter.MovQuaternionToUnityQuaternion(new GameBoxQuaternion()
                    {
                        X = reader.ReadSingle(),
                        Y = reader.ReadSingle(),
                        Z = reader.ReadSingle(),
                        W = reader.ReadSingle(),
                    }),
                    Scale = new []
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