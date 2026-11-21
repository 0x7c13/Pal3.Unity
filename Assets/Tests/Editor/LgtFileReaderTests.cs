// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Core.Tests
{
    using System.IO;
    using DataReader;
    using DataReader.Lgt;
    using NUnit.Framework;
    using Primitives;

    public sealed class LgtFileReaderTests
    {
        [TestCase(false)]
        [TestCase(true)]
        public void NativeInt32BooleanFieldsPreserveTheFollowingFieldsAndRecord(bool unsafeReader)
        {
            using var stream = new MemoryStream();
            using (var writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, true))
            {
                writer.Write(2);
                for (int index = 0; index < 2; index++)
                {
                    for (int element = 0; element < 16; element++)
                        writer.Write(element % 5 == 0 ? 1f : 0f);
                    writer.Write(index == 0 ? 0 : 2);
                    for (int component = 0; component < 8; component++)
                        writer.Write((float)(component + index));
                    writer.Write(index == 0 ? -1 : 0); // gbBOOL is signed int32, not a serialized byte.
                    writer.Write(index == 0 ? 0 : 1);
                    foreach (float distance in new[] { 11f, 22f, 33f, 44f })
                        writer.Write(distance + index);
                    writer.Write(2);
                    writer.Write(40f + index);
                    writer.Write(1);
                    writer.Write(70f + index);
                    writer.Write(80f + index);
                    writer.Write(1.25f + index);
                }
            }
            Assert.That(stream.Length, Is.EqualTo(4 + 2 * 148));
            using IBinaryReader reader = unsafeReader ? new UnsafeBinaryReader(stream.ToArray()) : new SafeBinaryReader(stream.ToArray());
            LgtFile file = new LgtFileReader().Read(reader, 936);
            Assert.That(reader.Position, Is.EqualTo(reader.Length));
            Assert.That(file.LightNodes.Length, Is.EqualTo(2));
            for (int index = 0; index < 2; index++)
            {
                LightNode light = file.LightNodes[index];
                Assert.That(light.UseDiffuse, Is.EqualTo(index == 0));
                Assert.That(light.UseSpecular, Is.EqualTo(index == 1));
                Assert.That(light.LightType, Is.EqualTo(index == 0 ? GameBoxLightType.Omni : GameBoxLightType.Directional));
                Assert.That(light.NearStart, Is.EqualTo(11 + index));
                Assert.That(light.FarEnd, Is.EqualTo(44 + index));
                Assert.That(light.LightDecayType, Is.EqualTo(GameBoxLightDecayType.Square));
                Assert.That(light.DecayRadius, Is.EqualTo(40 + index));
                Assert.That(light.LightShapeType, Is.EqualTo(GameBoxLightShapeType.Square));
                Assert.That(light.Size, Is.EqualTo(70 + index));
                Assert.That(light.Falloff, Is.EqualTo(80 + index));
                Assert.That(light.AspectRatio, Is.EqualTo(1.25f + index));
            }
        }

        [TestCase(-1)]
        [TestCase(1)]
        [TestCase(int.MaxValue)]
        public void RejectsCountsThatCannotFitTheRemainingFile(int count)
        {
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);
            writer.Write(count);
            using IBinaryReader reader = new SafeBinaryReader(stream.ToArray());
            Assert.Throws<InvalidDataException>(() => new LgtFileReader().Read(reader, 936));
        }
    }
}
