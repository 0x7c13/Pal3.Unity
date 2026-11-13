// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Core.Tests
{
    using System.IO;
    using DataReader;
    using DataReader.Nav;
    using NUnit.Framework;

    public sealed class NavFileReaderTests
    {
        private static byte[] Fixture(byte version, int width = 2) => ProtocolTestData.Build(w =>
        {
            w.Write(new byte[] { 78, 65, 86, 0, version, 2 }); w.Write(64u); w.Write(512u);
            w.At(64);
            for (int layer = 0; layer < 2; layer++)
            {
                if (version == 2) for (int i = 0; i < 32; i++) w.Write(layer * 100 + i);
                for (int i = 0; i < 6; i++) w.Write((float)(layer * 10 + i));
                w.Write(width); w.Write(1);
                for (int i = 0; i < 2; i++) { w.Write(layer * 10 + i + .5f); w.Write((byte)(i + 3)); w.Write((byte)(i + 5)); w.Write((ushort)0xABCD); }
            }
            w.At(512);
            for (int layer = 0; layer < 2; layer++)
            {
                w.Write((ushort)3); w.Write((ushort)1);
                foreach (float f in new[] { 0f, 0f, 0f, 1f, 0f, 0f, 0f, 0f, 1f }) w.Write(f);
                // 0,1,2 points down; the other layer already points up.
                w.Write((ushort)0); w.Write((ushort)(layer == 0 ? 1 : 2)); w.Write((ushort)(layer == 0 ? 2 : 1));
            }
        });

        [TestCase(false, 1)] [TestCase(true, 1)] [TestCase(false, 2)] [TestCase(true, 2)]
        public void VersionsLayersOffsetsTileAlignmentAndUpwardWindingArePreserved(bool unsafeReader, byte version)
        {
            byte[] data = Fixture(version);
            using IBinaryReader r = ProtocolTestData.Reader(data, unsafeReader);
            var nav = new NavFileReader().Read(r, 936);
            Assert.That(nav.Layers.Length, Is.EqualTo(2));
            Assert.That(nav.Layers[1].Portals.Length, Is.EqualTo(version == 2 ? 8 : 0));
            if (version == 2) Assert.That(nav.Layers[1].Portals[7].Bottom, Is.EqualTo(131));
            Assert.That(nav.Layers[1].Tiles[1].GameBoxYPosition, Is.EqualTo(11.5f));
            Assert.That(nav.Layers[1].Tiles[1].DistanceToNearestObstacle, Is.EqualTo(4));
            Assert.That((int)nav.Layers[1].Tiles[1].FloorType, Is.EqualTo(6));
            Assert.That(nav.MeshData[0].GameBoxTriangles, Is.EqualTo(new[] { 1, 0, 2 }));
            Assert.That(nav.MeshData[1].GameBoxTriangles, Is.EqualTo(new[] { 0, 2, 1 }));
            Assert.That(r.Position, Is.EqualTo(data.Length));
        }

        [TestCase(0)] [TestCase(3)] [TestCase(255)]
        public void UnknownVersionIsRejected(byte version)
        {
            using IBinaryReader r = new SafeBinaryReader(Fixture(version));
            Assert.Throws<InvalidDataException>(() => new NavFileReader().Read(r, 936));
        }

        [TestCase(0)] [TestCase(-2)] [TestCase(int.MaxValue)]
        public void InvalidDimensionsCannotOverflowOrAllocateFromUntrustedCounts(int width)
        {
            using IBinaryReader r = new UnsafeBinaryReader(Fixture(1, width));
            Assert.Throws<InvalidDataException>(() => new NavFileReader().Read(r, 936));
        }
    }
}
