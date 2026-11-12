// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Core.Tests
{
    using System;
    using System.IO;
    using DataReader;
    using DataReader.Sce;
    using NUnit.Framework;

    public sealed class SceFileReaderTests
    {
        private static byte[] Fixture() => ProtocolTestData.Build(w =>
        {
            w.Write(new byte[] { 83, 67, 69, 0, 1 }); w.Write((ushort)2);
            // Intentionally reverse physical block order; offsets, not table order, locate each block.
            w.Write(7u); w.Write(400u); w.FixedString("后块", 64);
            w.Write(9u); w.Write(256u); w.FixedString("前块", 64);
            w.At(256); Block(w, 9, 123); w.At(400); Block(w, 7, -456);
        });
        private static void Block(BinaryWriter w, uint id, int value)
        {
            w.Write(id); w.Write((ushort)5); w.FixedString("脚本", 5);
            w.Write((ushort)1); w.Write((byte)0); w.Write((ushort)4); w.Write(value);
            w.Write(4u); w.Write(new byte[] { 1, 2, 3, (byte)id });
        }

        [TestCase(false)] [TestCase(true)]
        public void IndexOffsetsDescriptionsVariablesAndScriptBytesArePreserved(bool unsafeReader)
        {
            using IBinaryReader r = ProtocolTestData.Reader(Fixture(), unsafeReader);
            var file = new SceFileReader().Read(r, 936);
            Assert.That(file.Indexes[0].Id, Is.EqualTo(7)); Assert.That(file.Indexes[1].Offset, Is.EqualTo(256));
            Assert.That(file.Indexes[0].Description, Is.EqualTo("后块"));
            Assert.That(file.ScriptBlocks[7].Description, Is.EqualTo("脚本"));
            Assert.That(file.ScriptBlocks[7].ScriptData, Is.EqualTo(new byte[] { 1, 2, 3, 7 }));
            Assert.That(BitConverter.ToInt32(file.ScriptBlocks[9].UserVariables[0].InitData, 0), Is.EqualTo(123));
        }

        [TestCase(false)] [TestCase(true)]
        public void TruncatedScriptPayloadIsRejected(bool unsafeReader)
        {
            byte[] data = Fixture(); Array.Resize(ref data, data.Length - 1);
            using IBinaryReader r = ProtocolTestData.Reader(data, unsafeReader);
            Assert.Throws<EndOfStreamException>(() => new SceFileReader().Read(r, 936));
        }

        [TestCase(0, 0)] [TestCase(4, 2)] [TestCase(11, int.MaxValue)]
        public void InvalidMagicVersionOrOffsetIsRejected(int offset, int value)
        {
            byte[] data = Fixture(); Buffer.BlockCopy(BitConverter.GetBytes(value), 0, data, offset, offset == 4 ? 1 : 4);
            using IBinaryReader r = new SafeBinaryReader(data);
            Assert.Throws<InvalidDataException>(() => new SceFileReader().Read(r, 936));
        }
    }
}
