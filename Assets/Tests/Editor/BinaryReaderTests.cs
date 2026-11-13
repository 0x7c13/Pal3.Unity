// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Core.Tests
{
    using System;
    using System.IO;
    using DataReader;
    using DataReader.Cpk;
    using NUnit.Framework;
    using Utilities;

    public sealed class BinaryReaderTests
    {
        [TestCase(false)] [TestCase(true)]
        public void LittleEndianValuesAndBulkReadsHaveExactPositions(bool unsafeReader)
        {
            byte[] data = ProtocolTestData.Build(w =>
            {
                w.Write((byte)0xFE); w.Write((short)-1234); w.Write((ushort)54321);
                w.Write(-1234567); w.Write(0xFEDCBA98u); w.Write(long.MinValue + 7); w.Write(ulong.MaxValue - 7);
                w.Write(1.25f); w.Write(-2.5d); w.Write((byte)2);
                w.Write(3.5f); w.Write(-4.5f); w.Write(5.5f);
                w.Write(new byte[] { 10, 20, 30, 40 });
            });
            using IBinaryReader r = ProtocolTestData.Reader(data, unsafeReader);
            Assert.That(r.ReadByte(), Is.EqualTo(254)); Assert.That(r.ReadInt16(), Is.EqualTo(-1234));
            Assert.That(r.ReadUInt16(), Is.EqualTo(54321)); Assert.That(r.ReadInt32(), Is.EqualTo(-1234567));
            Assert.That(r.ReadUInt32(), Is.EqualTo(0xFEDCBA98u)); Assert.That(r.ReadInt64(), Is.EqualTo(long.MinValue + 7));
            Assert.That(r.ReadUInt64(), Is.EqualTo(ulong.MaxValue - 7)); Assert.That(r.ReadSingle(), Is.EqualTo(1.25f));
            Assert.That(r.ReadDouble(), Is.EqualTo(-2.5d)); Assert.That(r.ReadBoolean(), Is.True);
            Assert.That(r.ReadSingles(3), Is.EqualTo(new[] { 3.5f, -4.5f, 5.5f }));
            var color = r.ReadColor32(); Assert.That(new[] { color.R, color.G, color.B, color.A }, Is.EqualTo(new byte[] { 10, 20, 30, 40 }));
            Assert.That(r.Position, Is.EqualTo(data.Length));
            r.Seek(-4, SeekOrigin.End); Assert.That(r.ReadBytes(8), Is.EqualTo(new byte[] { 10, 20, 30, 40 }));
            Assert.That(r.ReadBytes(1), Is.Empty);
            r.Seek(0, SeekOrigin.Begin); r.Seek(1, SeekOrigin.Current); Assert.That(r.ReadInt16(), Is.EqualTo(-1234));
        }

        [TestCase(false, 936, "仙剑三")] [TestCase(true, 936, "仙剑三")]
        [TestCase(false, 950, "仙劍三")] [TestCase(true, 950, "仙劍三")]
        public void FixedStringConsumesPaddingAndHandlesMissingTerminator(bool unsafeReader, int codepage, string text)
        {
            var data = ProtocolTestData.Build(w => { w.FixedString(text, 10, codepage); w.FixedString(text, 6, codepage); w.Write(0x12345678); });
            using IBinaryReader r = ProtocolTestData.Reader(data, unsafeReader);
            Assert.That(r.ReadString(10, codepage), Is.EqualTo(text)); Assert.That(r.Position, Is.EqualTo(10));
            Assert.That(r.ReadString(6, codepage), Is.EqualTo(text)); Assert.That(r.ReadInt32(), Is.EqualTo(0x12345678));
        }

        [TestCase(false)] [TestCase(true)]
        public void TruncatedScalarsAndBulkArraysCannotReadOutsideInput(bool unsafeReader)
        {
            using IBinaryReader r = ProtocolTestData.Reader(new byte[3], unsafeReader);
            Assert.Throws<EndOfStreamException>(() => r.ReadInt32());
            r.Seek(0, SeekOrigin.Begin); Assert.Throws<EndOfStreamException>(() => r.ReadSingles(2));
            r.Seek(0, SeekOrigin.Begin); Assert.That(r.ReadBytes(10).Length, Is.EqualTo(3));
            Assert.Throws<EndOfStreamException>(() => r.ReadByte());
            Assert.Throws<ArgumentOutOfRangeException>(() => r.ReadBytes(-1));
        }

        [Test]
        public void UnsafeStreamConstructorReadsAllShortReadsAndPreservesInitialPosition()
        {
            using var stream = new ProtocolTestData.ShortReadStream(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 });
            stream.Position = 2;
            using IBinaryReader r = new UnsafeBinaryReader(stream);
            Assert.That(r.Position, Is.EqualTo(2)); Assert.That(r.Length, Is.EqualTo(8));
            Assert.That(r.ReadBytes(6), Is.EqualTo(new byte[] { 3, 4, 5, 6, 7, 8 }));
            r.Seek(0, SeekOrigin.Begin); Assert.That(r.ReadBytes(2), Is.EqualTo(new byte[] { 1, 2 }));
        }

        [Test]
        public void EmptyUnsafeReaderAndRepeatedDisposeAreSafe()
        {
            var reader = new UnsafeBinaryReader(Array.Empty<byte>());
            Assert.That(reader.Position, Is.Zero); Assert.That(reader.ReadBytes(0), Is.Empty);
            Assert.Throws<EndOfStreamException>(() => reader.ReadByte());
            reader.Dispose(); reader.Dispose();
            Assert.Throws<ObjectDisposedException>(() => reader.ReadByte());
        }

        [Test]
        public void StructReadsHandleShortStreamsAndRejectTruncatedSpans()
        {
            byte[] data = ProtocolTestData.Build(w => { for (uint i = 1; i <= 7; i++) w.Write(i); });
            using var stream = new ProtocolTestData.ShortReadStream(data);
            var record = CoreUtility.ReadStruct<CpkTableEntity>(stream);
            Assert.That(record.CRC, Is.EqualTo(1)); Assert.That(record.ExtraInfoSize, Is.EqualTo(7));
            Assert.That(stream.Position, Is.EqualTo(28));
            Assert.Throws<EndOfStreamException>(() => CoreUtility.ReadStruct<CpkTableEntity>(data.AsSpan(0, 27)));
        }
    }
}
