// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Core.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text;
    using DataReader.Cpk;
    using DataReader.Cpk.Lzo;
    using FileSystem;
    using NUnit.Framework;

    public sealed class CpkTests
    {
        private string _root;
        [SetUp] public void SetUp() { _root = Path.Combine(Path.GetTempPath(), "pal3-cpk-" + Guid.NewGuid()); Directory.CreateDirectory(_root); }
        [TearDown] public void TearDown() => Directory.Delete(_root, true);
        private static Crc32Hash Hash() { var hash = new Crc32Hash(); hash.Init(); return hash; }

        // Fixed vectors from the original CPK::Crc algorithm, not a standard CRC32 library.
        [TestCase("", 936, 0u)] [TestCase("a", 936, 0x61000000u)] [TestCase("abcd", 936, 0x61626364u)]
        [TestCase("abcde", 936, 0x7BF0FF0Eu)] [TestCase("仙剑三", 936, 0x43E5BA15u)]
        [TestCase("仙劍三", 950, 0x0E8E9A1Au)] [TestCase("data\\test.bin", 936, 0xBA7CB63Au)]
        [TestCase("abcd\0ignored", 936, 0x61626364u)]
        public void OriginalCrcVectorsIncludingChineseAndNullTerminationMatch(string text, int codepage, uint expected) =>
            Assert.That(Hash().Compute(text, codepage), Is.EqualTo(expected));

        [TestCase(false)] [TestCase(true)]
        public void SparseArchiveUsesTableOffsetSkipsDeletedEntriesAndReadsSameBytesAfterPreload(bool compressed)
        {
            string path = Fixture("test.cpk", "data\\test.bin", compressed);
            var archive = new CpkArchive(path, Hash(), 936); archive.Init();
            Assert.That(archive.FileExists("DATA/test.BIN", out uint hash), Is.True);
            Assert.That(hash, Is.EqualTo(0xBA7CB63Au));
            Assert.That(archive.ReadAllBytes(hash), Is.EqualTo(Encoding.ASCII.GetBytes("HELLO")));
            archive.LoadArchiveIntoMemory(); Assert.That(archive.ReadAllBytes(hash), Is.EqualTo(Encoding.ASCII.GetBytes("HELLO")));
            Assert.That(LocalGameDataTests.Files(archive.GetRootEntries()), Is.EqualTo(new[] { "data\\test.bin" }));
            archive.DisposeInMemoryArchive(); Assert.That(archive.ReadAllBytes(hash), Is.EqualTo(Encoding.ASCII.GetBytes("HELLO")));
            Assert.Throws<FileNotFoundException>(() => archive.ReadAllBytes(0xDEADBEEF));
            Assert.Throws<InvalidOperationException>(() => archive.ReadAllBytes(0x64617461));
        }

        [Test]
        public void VirtualPathsAreCaseAndSeparatorIndependentEvenInTurkishCulture()
        {
            Fixture("test.cpk", "data\\pi01.bin", false);
            var fs = new CpkFileSystem(_root, Hash(), 936); fs.Mount("test.cpk", 936);
            var culture = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = new CultureInfo("tr-TR");
                Assert.That(fs.FileExists("TEST.CPK/DATA/PI01.BIN"), Is.True);
                Assert.That(fs.ReadAllBytes("TEST.CPK\\DATA\\PI01.BIN"), Is.EqualTo(Encoding.ASCII.GetBytes("HELLO")));
            }
            finally { CultureInfo.CurrentCulture = culture; }
        }

        [Test]
        public void SegmentedArchiveResolvesLogicalNameAndReportsPhysicalArchive()
        {
            Fixture("q02_01.cpk", "q02_01.cpk\\data\\test.bin", false);
            var fs = new CpkFileSystem(_root, Hash(), 936); fs.Mount("q02_01.cpk", 936);
            Assert.That(fs.FileExists("Q02.CPK/Q02.CPK/DATA/TEST.BIN", out string archive), Is.True);
            Assert.That(archive, Is.EqualTo("q02_01.cpk"));
            Assert.That(fs.ReadAllBytes("q02.cpk\\q02.cpk\\data\\test.bin"), Is.EqualTo(Encoding.ASCII.GetBytes("HELLO")));
            Assert.That(fs.FileExists("q02.cpk\\missing.bin"), Is.False);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void MetadataReportsReadSizeWithoutReadingPayload(bool compressed)
        {
            string path = Fixture("q02_01.cpk", "data\\test.bin", compressed);
            var fs = new CpkFileSystem(_root, Hash(), 936);
            fs.Mount("q02_01.cpk", 936);
            File.Delete(path);
            var sizes = fs.GetFileSizes("Q02.CPK/DATA/TEST.BIN");
            Assert.That(sizes.length, Is.EqualTo(5));
            Assert.That(sizes.packedLength, Is.EqualTo(compressed ? 9 : 5));
            Assert.Throws<FileNotFoundException>(() => fs.GetFileSizes("q02.cpk/missing"));
        }

        [Test]
        public void TruncatedArchiveHeaderAndIndexTableAreRejected()
        {
            string path = Fixture("test.cpk", "data\\test.bin", false);
            byte[] data = File.ReadAllBytes(path);
            foreach (int length in new[] { 0, 127, 160 + 28 * 7 })
            {
                File.WriteAllBytes(path, data.Take(length).ToArray());
                var archive = new CpkArchive(path, Hash(), 936);
                Assert.That(() => archive.Init(), Throws.InstanceOf<EndOfStreamException>().Or.InstanceOf<InvalidDataException>());
            }
        }

        [Test]
        public void LzoRejectsShortInputAndIncorrectDeclaredOutputSize()
        {
            foreach (int length in new[] { 0, 1, 2 })
                Assert.Throws<OverflowException>(() => MiniLzo.Decompress(new byte[length], new byte[5]));
            byte[] compressed = { 22, 72, 69, 76, 76, 79, 17, 0, 0 };
            Assert.Throws<OverflowException>(() => MiniLzo.Decompress(compressed, new byte[10]));
            Assert.Throws<OverflowException>(() => MiniLzo.Decompress(compressed, new byte[4]));
            Assert.DoesNotThrow(() => MiniLzo.Decompress(new byte[] { 17, 0, 0 }, Array.Empty<byte>()));
        }

        private string Fixture(string name, string virtualPath, bool compressed)
        {
            var crc = Hash();
            var entries = new List<(uint crc, uint parent, uint flag, byte[] data, string name)>();
            string prefix = ""; uint parent = 0;
            string[] parts = virtualPath.Split('\\');
            for (int i = 0; i < parts.Length; i++)
            {
                prefix += (prefix.Length == 0 ? "" : "\\") + parts[i];
                uint key = crc.Compute(prefix, 936); bool directory = i < parts.Length - 1;
                byte[] payload = directory ? Array.Empty<byte>() : compressed ?
                    new byte[] { 22, 72, 69, 76, 76, 79, 17, 0, 0 } : Encoding.ASCII.GetBytes("HELLO");
                entries.Add((key, parent, directory ? 3u : compressed ? 1u : 0x10001u, payload, parts[i]));
                parent = key;
            }
            byte[] data = ProtocolTestData.Build(w =>
            {
                w.Write(0x1A545352u); w.Write(1u); w.Write(160u); w.Write(1024u); w.Write(8u); w.Write((uint)entries.Count);
                w.Write(0u); w.Write(128u); w.Write((uint)(entries.Count + 1)); w.Write(8u); w.Write(1u); w.Write(0u); w.Write(new byte[80]);
                w.At(160); w.Write(new byte[28]); // empty first slot
                uint start = 1024;
                foreach (var entry in entries)
                {
                    byte[] filename = Encoding.ASCII.GetBytes(entry.name + '\0');
                    w.Write(entry.crc); w.Write(entry.flag); w.Write(entry.parent); w.Write(start);
                    w.Write((uint)entry.data.Length); w.Write(entry.data.Length == 0 ? 0u : 5u); w.Write((uint)filename.Length);
                    long tablePosition = w.BaseStream.Position; w.At(start); w.Write(entry.data); w.Write(filename);
                    start += (uint)(entry.data.Length + filename.Length); w.BaseStream.Position = tablePosition;
                }
                w.Write(0xDEADBEEFu); w.Write(0x11u); w.Write(new byte[20]); // deleted
                w.Write(new byte[(8 - entries.Count - 2) * 28]);
            });
            string path = Path.Combine(_root, name); File.WriteAllBytes(path, data); return path;
        }
    }
}
