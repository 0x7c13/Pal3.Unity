// ------------------------------------------------------------------------
//  Copyright (c) 2021-2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Core.DataReader.Data
{
    using System;

    public sealed class EffectDefinitionFileReader : IFileReader<EffectDefinitionFile>
    {
        public const int Pal3Size = 1324 * 280 + 560 * 15940;
        public const int Pal3ASize = 2048 * 280 + 1024 * 15940;

        public EffectDefinitionFile Read(IBinaryReader reader, int codepage)
        {
            byte[] bytes = reader.ReadExactly(checked((int)(reader.Length - reader.Position)));
            // Validate the layout before accepting an unwrapped file. The shipped
            // PAL3 sample is a padded LZO stream with the same length as the table.
            if (bytes.Length == Pal3Size || bytes.Length == Pal3ASize)
            {
                try
                {
                    return ReadPayload(bytes, codepage);
                }
                catch (System.IO.InvalidDataException)
                {

                }
                var decoded = new byte[bytes.Length];
                Cpk.Lzo.MiniLzo.DecompressPrefix(bytes, decoded);
                return ReadPayload(decoded, codepage);
            }
            if (bytes.Length >= 7)
            {
                int size = bytes[0] | bytes[1] << 8 | bytes[2] << 16 | bytes[3] << 24;
                if (size == Pal3Size || size == Pal3ASize)
                {
                    var decoded = new byte[size];
                    Cpk.Lzo.MiniLzo.Decompress(bytes.AsSpan(4), decoded);
                    return ReadPayload(decoded, codepage);
                }
            }
            throw new System.IO.InvalidDataException("Unrecognized flexeff table layout or compression wrapper");
        }

        private EffectDefinitionFile ReadPayload(byte[] bytes, int codepage)
        {
            int typeCount = bytes.Length == Pal3Size ? 1324 : 2048;
            int groupCount = bytes.Length == Pal3Size ? 560 : 1024;
            using IBinaryReader reader = new SafeBinaryReader(bytes);
            var definitions = new EffectDefinition[typeCount];
            for (int i = 0; i < typeCount; i++)
            {
                // Invalid slots may contain uninitialized native bytes.
                long offset = reader.Position;
                reader.Seek(offset + 212, System.IO.SeekOrigin.Begin);
                byte valid = reader.ReadByte();
                if (valid > 1)
                    throw new System.IO.InvalidDataException($"Invalid effect type flag at {i}");
                reader.Seek(offset, System.IO.SeekOrigin.Begin);
                definitions[i] = ReadEffectDefinition(reader, codepage);
                if (valid == 1 && (float.IsNaN(definitions[i].InitSize) || float.IsInfinity(definitions[i].InitSize)))
                    throw new System.IO.InvalidDataException($"Invalid effect size at {i}");
            }
            var groups = new EffectGroup[groupCount];
            for (int g = 0; g < groupCount; g++)
            {
                int count = reader.ReadInt32();
                if (count < 0 || count > 16)
                    throw new System.IO.InvalidDataException($"Invalid effect group size at {g}");
                uint[] crcs = reader.ReadUInt32s(16);
                var objects = new EffectGroupObject[count];
                for (int o = 0; o < 16; o++)
                {
                    var tasks = new System.Collections.Generic.List<EffectTask>();
                    for (int t = 0; t < 8; t++)
                    {
                        if (o >= count)
                        {
                            reader.Seek(124, System.IO.SeekOrigin.Current);
                            continue;
                        }
                        EffectTask task = ReadTask(reader);
                        if (task.IsValid)
                            tasks.Add(task);
                    }
                    if (o < count)
                        objects[o] = new EffectGroupObject { TypeCrc = crcs[o], Tasks = tasks.ToArray() };
                }
                groups[g] = new EffectGroup { Objects = objects };
            }
            var file = new EffectDefinitionFile(definitions, groups);
            for (int g = 0; g < groups.Length; g++)
                foreach (EffectGroupObject obj in groups[g].Objects)
                    if (!file.TryGetType(obj.TypeCrc, out _))
                        throw new System.IO.InvalidDataException($"Group {g} references missing effect type {obj.TypeCrc:X8}");
            return file;
        }

        private static EffectTask ReadTask(IBinaryReader reader)
        {
            var task = new EffectTask
            {
                XStretchSpeed = reader.ReadSingle(),
                SpeedX = reader.ReadSingle(),
                SpeedY = reader.ReadSingle(),
                SpeedZ = reader.ReadSingle(),
                SpeedRot = reader.ReadSingle(),
                CircleAxisX = reader.ReadSingle(),
                CircleAxisY = reader.ReadSingle(),
                CircleAxisZ = reader.ReadSingle(),
                CircleRadius = reader.ReadSingle(),
                CircleSpeed = reader.ReadSingle(),
                RotAxisX = reader.ReadSingle(),
                RotAxisY = reader.ReadSingle(),
                RotAxisZ = reader.ReadSingle(),
                RotSpeed = reader.ReadSingle(),
                SpeedAlpha = reader.ReadSingle(),
                SpeedRed = reader.ReadSingle(),
                SpeedGreen = reader.ReadSingle(),
                SpeedBlue = reader.ReadSingle(),
                StartTime = reader.ReadSingle(),
                Duration = reader.ReadSingle(),
                RequireLoops = reader.ReadInt32(),
                CreateTimeSlot = reader.ReadSingle(),
            };
            byte valid = reader.ReadByte();
            if (valid > 1)
                throw new System.IO.InvalidDataException("Invalid task validity flag");
            task.IsValid = valid == 1;
            task.CreateNew = reader.ReadBoolean();
            task.ModifyLast = reader.ReadBoolean();
            reader.ReadByte(); // native 4-byte alignment
            task.YStretchSpeed = reader.ReadSingle();
            reader.Seek(28, System.IO.SeekOrigin.Current); // reserved, not active parameters
            if (task.IsValid && (!Finite(task.StartTime) || !Finite(task.Duration) || task.Duration < 0))
                throw new System.IO.InvalidDataException("Invalid effect task timing");
            return task;
        }

        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);

        private EffectDefinition ReadEffectDefinition(IBinaryReader reader, int codepage)
        {
            EffectDefinition effectDefinition = new()
            {
                ClassNameCrc = reader.ReadUInt32(),
                TypeName = reader.ReadString(32, codepage),
                TypeNameCrc = reader.ReadUInt32()
            };

            // union
            {
                string unionStr = reader.ReadString(128, codepage);
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
                float unionFloat = reader.ReadSingle();
                effectDefinition.InitYRotation = unionFloat;
                effectDefinition.InitAngle = unionFloat;
            }

            effectDefinition.InitSize = reader.ReadSingle();
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
