// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Core.Tests
{
    using System;
    using System.IO;
    using System.Text;
    using DataReader;
    using DataReader.Data;
    using DataReader.Par;
    using NUnit.Framework;

    public sealed class SparFormatTests
    {
        [Test]
        public void SparClassNameMatchesTheOriginalRegistration()
        {
            Assert.That(EffectClass.Name(0x8438975e), Is.EqualTo("S-Particle"));
        }

        [Test]
        public void SparReadsKeyframeShapesControllersAndEditorTail()
        {
            byte[] bytes = Fixture();
            var file = ((IFileReader<SparFile>)new SparFileReader()).Read(bytes, 936);
            Assert.That(file.Groups.Length, Is.EqualTo(1));
            SparGroup group = file.Groups[0];
            Assert.That(group.Name, Is.EqualTo("synthetic"));
            Assert.That(group.Texture, Is.EqualTo("Media\\test.tga"));
            Assert.That(group.Capacity, Is.EqualTo(32));
            Assert.That(group.Loop, Is.True);
            Assert.That(group.TextureFrames[0], Is.EqualTo(new[] { 0f, 0f, 1f, 1f }));
            Assert.That(group.Keyframes[0].Time, Is.EqualTo(1.25f));
            Assert.That(group.Keyframes[0].ShapeParameters, Is.EqualTo(new[] { 10f }));
            Assert.That(group.Keyframes[0].VelocityMaximum.Z, Is.EqualTo(6));
            Assert.That(group.Keyframes[0].FrameMinimum, Is.EqualTo(5));
            Assert.That(group.Keyframes[0].FrameMaximum, Is.EqualTo(15));
            Assert.That(group.Keyframes[0].Controllers[0].Parameters, Is.EqualTo(new[] { 1f, 2f, 3f }));
            Assert.That(file.Shader, Is.EqualTo(1));
            Assert.That(file.UnusedByteCount, Is.EqualTo(3));
        }

        [Test]
        public void SparRejectsTruncationInvalidHeaderCountsAndControllerKinds()
        {
            IFileReader<SparFile> reader = new SparFileReader();
            byte[] bytes = Fixture();
            Assert.Throws<EndOfStreamException>(() => reader.Read(bytes[..900], 936));
            byte[] changed = (byte[])bytes.Clone();
            changed[8] = 0;
            Assert.Throws<InvalidDataException>(() => reader.Read(changed, 936));
            changed = (byte[])bytes.Clone();
            BitConverter.GetBytes(int.MaxValue).CopyTo(changed, 4);
            Assert.Throws<InvalidDataException>(() => reader.Read(changed, 936));
            changed = (byte[])bytes.Clone();
            BitConverter.GetBytes(99).CopyTo(changed, 808 + 16 + 28 + 4 + 4 + 96);
            Assert.Throws<InvalidDataException>(() => reader.Read(changed, 936));
        }

        private static byte[] Fixture()
        {
            using var stream = new MemoryStream();
            using var w = new BinaryWriter(stream);
            w.Write(new byte[264 + 544]);
            stream.Position = 0;
            w.Write(1f);
            w.Write(1);
            w.Write(Encoding.ASCII.GetBytes("__PARTICLE_EFFECT(Softstar_Ltd)_EXPORT__"));
            stream.Position = 268;
            w.Write(32);
            w.Write(192);
            w.Write(0);
            w.Write(1);
            w.Write(1);
            w.Write(1);
            w.Write(Encoding.ASCII.GetBytes("synthetic"));
            stream.Position = 264 + 284;
            w.Write(Encoding.ASCII.GetBytes("Media\\test.tga"));
            stream.Position = 808;
            foreach (float value in new[] { 0f, 0f, 1f, 1f })
                w.Write(value);
            w.Write(1.25f);
            w.Write(new byte[12]);
            w.Write(15);
            w.Write(30);
            w.Write(1);
            w.Write(10f);
            w.Write(0);
            for (int i = 1; i <= 22; i++)
                w.Write((float)i);
            w.Write(5);
            w.Write(15);
            w.Write(2);
            w.Write(1f);
            w.Write(2f);
            w.Write(3f);
            w.Write(new byte[5 * 4]);
            w.Write(new byte[16]);
            w.Write(1);
            w.Write(new byte[16]);
            w.Write(new byte[3]);
            return stream.ToArray();
        }
    }
}
