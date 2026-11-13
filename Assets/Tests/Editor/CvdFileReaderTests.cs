// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Core.Tests
{
    using DataReader;
    using DataReader.Cvd;
    using NUnit.Framework;

    public sealed class CvdFileReaderTests
    {
        [TestCase(false, "cvdf")] [TestCase(true, "cvdf")]
        [TestCase(false, "cvds")] [TestCase(true, "cvds")]
        public void NonzeroMeshAndMaterialTimesAreRebasedWithoutChangingIntervals(bool unsafeReader, string magic)
        {
            byte[] data = ProtocolTestData.Build(w =>
            {
                w.Write(System.Text.Encoding.ASCII.GetBytes(magic)); w.Write(1); w.Write((byte)1);
                // Original gbcontroler: max slot sizes are 44 bytes for position/rotation, 60 for scale.
                for (int track = 0; track < 3; track++)
                {
                    w.Write(1); w.Write((byte)3); w.Write(0f); w.Write(0u);
                    w.Write(new byte[track == 2 ? 52 : 36]);
                }
                w.Write(1f); w.Write(3); w.Write(3);
                for (int frame = 0; frame < 3; frame++)
                    for (int vertex = 0; vertex < 3; vertex++)
                        for (int component = 0; component < 8; component++) w.Write((float)(100 * frame + 10 * vertex + component));
                w.Write(10f); w.Write(12f); w.Write(15f);
                w.Write(1); w.Write((byte)0); w.Write(new byte[16]); w.Write(8f); w.FixedString("test.dds", 64);
                w.Write(1); w.Write((ushort)2); w.Write((ushort)0); w.Write((ushort)1);
                if (magic == "cvds")
                {
                    w.Write(3); w.Write(20f); w.Write(23f); w.Write(26f);
                    for (int frame = 0; frame < 3; frame++) for (int c = 0; c < 17; c++) w.Write((float)(frame * 100 + c));
                }
                for (int i = 0; i < 16; i++) w.Write(i % 5 == 0 ? 1f : 0f);
                w.Write(0); // child nodes
            });
            using IBinaryReader r = ProtocolTestData.Reader(data, unsafeReader);
            var file = new CvdFileReader().Read(r, 936);
            var mesh = file.RootNodes[0].Mesh;
            Assert.That(mesh.AnimationTimeKeys, Is.EqualTo(new[] { 0f, 2f, 5f }));
            var section = mesh.MeshSections[0];
            Assert.That(section.AnimationTimeKeys, Is.EqualTo(magic == "cvds" ? new[] { 0f, 3f, 6f } : new float[0]));
            Assert.That(section.GameBoxTriangles, Is.EqualTo(new[] { 0, 1, 2 }));
            Assert.That(section.FrameVertices[2][0].GameBoxPosition.X, Is.EqualTo(225f));
            if (magic == "cvds") Assert.That(section.AnimationMaterials[2].SpecularPower, Is.EqualTo(216f));
            Assert.That(r.Position, Is.EqualTo(data.Length));
        }
    }
}
