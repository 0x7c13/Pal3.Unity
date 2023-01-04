// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

//#define USE_UNSAFE_BINARY_READER

namespace Core.DataReader.Data
{
    using System.IO;
    using Extensions;
    using Utils;

    public static class EffectDefinitionFileReader
    {
        private const int NUM_OF_EFFECTS = 1324;

        public static void Read(byte[] data, int codepage)
        {
            #if USE_UNSAFE_BINARY_READER
            using var reader = new UnsafeBinaryReader(data);
            #else
            using var stream = new MemoryStream(data);
            using var reader = new BinaryReader(stream);
            #endif

            var effectDefinitions = new EffectDefinition[NUM_OF_EFFECTS];

            for (var i = 0; i < NUM_OF_EFFECTS; i++)
            {
                effectDefinitions[i] = ReadEffectDefinition(reader, codepage);
            }
        }

        #if USE_UNSAFE_BINARY_READER
        private static EffectDefinition ReadEffectDefinition(UnsafeBinaryReader reader, int codepage)
        #else
        private static EffectDefinition ReadEffectDefinition(BinaryReader reader, int codepage)
        #endif
        {
            var effectDefinition = new EffectDefinition();

            effectDefinition.ClassNameCrc = reader.ReadUInt32();
            effectDefinition.TypeName = reader.ReadString(32, 936);
            effectDefinition.TypeNameCrc = reader.ReadUInt32();

            // union
            {
                var unionStr = reader.ReadString(128, 936);
                effectDefinition.TextureName = unionStr;
                effectDefinition.ParticleFileName = unionStr;
                effectDefinition.SfxFileNamePrefix = unionStr;
                effectDefinition.VideoFileNamePrefix = unionStr;
            }

            effectDefinition.FramesPerRow = reader.ReadInt32();
            effectDefinition.FramesPerColumn = reader.ReadInt32();
            effectDefinition.FrameCount = reader.ReadInt32();
            effectDefinition.FramePerSecond = reader.ReadSingle();

            effectDefinition.InitXPosition = reader.ReadSingle();
            effectDefinition.InitYPosition = reader.ReadSingle();
            effectDefinition.InitZPosition = reader.ReadSingle();

            // union
            {
                var unionFloat = reader.ReadSingle();
                effectDefinition.InitYRotation = unionFloat;
                effectDefinition.InitAngle = unionFloat;
            }

            effectDefinition.InitSize = reader.ReadInt32();
            effectDefinition.InitColor = reader.ReadUInt32();
            effectDefinition.MaxQuadCount = reader.ReadInt32();

            effectDefinition.IsValid = reader.ReadBoolean();
            effectDefinition.IsAdditiveBlend = reader.ReadBoolean();
            effectDefinition.IsBillboard = reader.ReadBoolean();
            effectDefinition.IsZTestIgnored = reader.ReadBoolean();

            effectDefinition.InitXRotation = reader.ReadSingle();
            effectDefinition.InitZRotation = reader.ReadSingle();

            effectDefinition.RotationAngle = reader.ReadSingle();
            effectDefinition.Width = reader.ReadSingle();
            effectDefinition.DivisionCount = reader.ReadSingle();
            effectDefinition.TopShrink = reader.ReadSingle();
            effectDefinition.BottomShrink = reader.ReadSingle();
            effectDefinition.TopRadius = reader.ReadSingle();
            effectDefinition.BottomRadius = reader.ReadSingle();
            effectDefinition.TopRadiusIncreaseSpeed = reader.ReadSingle();
            effectDefinition.BottomRadiusIncreaseSpeed = reader.ReadSingle();
            effectDefinition.TopRiseSpeed = reader.ReadSingle();
            effectDefinition.BottomRiseSpeed = reader.ReadSingle();

            effectDefinition.TextureRepeat = reader.ReadSingle();

            effectDefinition.Param = new[]
            {
                reader.ReadSingle(),
                reader.ReadSingle(),
            };

            return effectDefinition;
        }
    }
}