// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Core.Tests
{
    using System.IO;
    using Core.Contract.Enums;
    using Core.DataReader;
    using Core.DataReader.Gdb;
    using Core.DataReader.Mv3;
    using NUnit.Framework;

    public sealed class BattleDataReaderTests
    {
        [TestCase("animation event", int.MaxValue)]
        [TestCase("vertex", int.MaxValue)]
        [TestCase("vertex frame", int.MaxValue)]
        [TestCase("vertex frame", -1)]
        [TestCase("texture coordinate", int.MaxValue)]
        [TestCase("triangle", int.MaxValue)]
        [TestCase("command", int.MaxValue)]
        public void Mv3RejectsCountsThatDoNotFitInTheFile(string field, int value)
        {
            byte[] data = Mv3Fixture(field, value);
            var reader = (IFileReader<Mv3File>)new Mv3FileReader();
            var error = Assert.Throws<InvalidDataException>(() => reader.Read(data, 936));
            Assert.That(error.Message, Does.Contain(field + " count"));
        }

        [TestCase("vertex", false)]
        [TestCase("vertex", true)]
        [TestCase("texture coordinate", false)]
        [TestCase("texture coordinate", true)]
        public void Mv3RejectsOutOfRangeTriangleCornersWithEitherNormalMode(string field, bool nativeNormals)
        {
            byte[] data = Mv3Fixture(field + " index", ushort.MaxValue);
            var reader = (IFileReader<Mv3File>)new Mv3FileReader(nativeNormals);
            var error = Assert.Throws<InvalidDataException>(() => reader.Read(data, 936));
            Assert.That(error.Message, Does.Contain(field + " index"));
        }

        private static byte[] Mv3Fixture(string field, int invalid)
        {
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);
            void Count(string name, int value) => writer.Write(field == name ? invalid : value);
            void Index(string name, ushort value) => writer.Write(field == name + " index" ? (ushort)invalid : value);

            writer.Write(new byte[] { (byte)'M', (byte)'V', (byte)'3', 0 });
            writer.Write(100);
            writer.Write(0u);
            Count("material", 1);
            Count("tag node", 0);
            Count("mesh", 1);
            Count("animation event", 0);
            writer.Write(new byte[84]); // material and empty texture names
            writer.Write(new byte[64]); // mesh name
            Count("vertex", 3);
            writer.Write(new byte[24]); // bounds
            Count("vertex frame", 1);
            writer.Write(new byte[28]); // packed frame
            Count("texture coordinate", 0);
            Count("mesh attribute", 1);
            writer.Write(0); // material index
            Count("triangle", 1);
            Index("vertex", 0);
            writer.Write((ushort)1);
            writer.Write((ushort)2);
            Index("texture coordinate", 0);
            writer.Write(new byte[4]); // remaining UV corners
            Count("command", 0);
            return stream.ToArray();
        }

        [Test]
        public void ComboValuesUseTheNativeShortAlignmentAndKeepTheNextRecordAligned()
        {
#if PAL3
            const int stride = 0x278;
#else
            const int stride = 0x27c;
#endif
            var bytes = new byte[160 + stride * 2];
            using (var writer = new BinaryWriter(new MemoryStream(bytes)))
            {
                writer.BaseStream.Position = 128;
                for (int section = 0; section < 4; section++)
                {
                    writer.Write(160);
                    writer.Write(section == 3 ? 2 : 0);
                }
                for (int record = 0; record < 2; record++)
                {
                    int offset = 160 + record * stride;
                    writer.BaseStream.Position = offset + 32;
                    writer.Write(501 + record);
                    writer.BaseStream.Position = offset + 68;
                    writer.Write(-1);
                    writer.Write(0);
                    writer.Write(26);
                    writer.BaseStream.Position = offset + 0x250;
                    writer.Write((byte)(record == 0 ? 0 : 3));
                    writer.BaseStream.Position = offset + 0x25d;
                    writer.Write((byte)0x7f); // Alignment bytes must not become a resource value.
                    for (int attribute = 0; attribute < 12; attribute++)
                        writer.Write((short)(attribute == 0 ? -300 - record : record == 1 && attribute == 4 ? 0 : attribute + record + 1));
                    writer.Write((short)0x1234); // Record-tail alignment is also excluded.
#if PAL3A
                    writer.Write(12345 + record);
#endif
                }
            }
            GdbFile database = ((IFileReader<GdbFile>)new GdbFileReader()).Read(bytes, 936);
            Assert.That(database.ComboSkillInfos.Count, Is.EqualTo(2));
            Assert.That(database.ComboSkillOrder, Is.EqualTo(new[] { 501, 502 }));
            for (int record = 0; record < 2; record++)
            {
                ComboSkillInfo combo = database.ComboSkillInfos[501 + record];
                Assert.That(combo.AuxiliaryStateConditions, Is.EqualTo(new[] { -1, 0, 26 }));
                Assert.That(combo.TargetRangeType, Is.EqualTo(record == 0 ? ComboTargetRangeType.PreserveSingleTarget : ComboTargetRangeType.OpponentAll));
                Assert.That(combo.AttributeImpacts.Count, Is.EqualTo(12));
                for (int attribute = 0; attribute < 12; attribute++)
                    Assert.That(combo.AttributeImpacts[(ActorAttributeType)attribute].Value,
                        Is.EqualTo(attribute == 0 ? -300 - record : record == 1 && attribute == 4 ? 0 : attribute + record + 1));
#if PAL3A
                Assert.That(combo.PowerMultiplierPercentage, Is.EqualTo(12345 + record));
#endif
            }
        }
    }
}
