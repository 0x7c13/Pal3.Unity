// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2024, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Core.DataReader.Data
{
    public sealed class EffectDefinitionFileReader : IFileReader<EffectDefinitionFile>
    {
        private const int NUM_OF_EFFECTS = 1324;

        public EffectDefinitionFile Read(IBinaryReader reader, int codepage)
        {
            var effectDefinitions = new EffectDefinition[NUM_OF_EFFECTS];

            for (var i = 0; i < NUM_OF_EFFECTS; i++)
            {
                effectDefinitions[i] = ReadEffectDefinition(reader, codepage);
            }

            return new EffectDefinitionFile(effectDefinitions);
        }

        private EffectDefinition ReadEffectDefinition(IBinaryReader reader, int codepage)
        {
            var effectDefinition = new EffectDefinition
            {
                ClassNameCrc = reader.ReadUInt32(),
                TypeName = reader.ReadString(32, codepage),
                TypeNameCrc = reader.ReadUInt32()
            };

            // union
            {
                var unionStr = reader.ReadString(128, codepage);
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