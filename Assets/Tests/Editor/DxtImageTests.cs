// See LICENSE in the project root.

namespace Pal3.Core.Tests
{
    using System;
    using System.IO;
    using DataReader.Dxt;
    using NUnit.Framework;

    public sealed class DxtImageTests
    {
        [TestCase(0, 0)] // Magic
        [TestCase(4, 120)] // Header size
        [TestCase(76, 28)] // Pixel format size
        [TestCase(80, 0)] // Missing FourCC flag
        [TestCase(84, 0)] // Unsupported encoding
        [TestCase(16, 0)] // Width
        [TestCase(12, -1)] // Height
        [TestCase(28, 4)] // Too many mips for 4x4
        [TestCase(24, 2)] // Volume depth
        [TestCase(112, 512)] // Cubemap caps
        public void InvalidHeaderIsRejectedBeforeDecoding(int offset, int value)
        {
            byte[] data = CreateDds(1);
            WriteInt(data, offset, value);
            Assert.Throws<InvalidDataException>(() => new DxtImage(data));
        }

        [TestCase(0)]
        [TestCase(127)]
        [TestCase(128)]
        [TestCase(143)]
        public void TruncatedHeaderOrBaseLevelIsRejected(int length)
        {
            byte[] data = CreateDds(1);
            Array.Resize(ref data, length);
            Assert.Throws<InvalidDataException>(() => new DxtImage(data));
        }

        [Test]
        public void TruncatedLaterMipIsRejected()
        {
            byte[] data = CreateDds(2);
            Array.Resize(ref data, data.Length - 1);
            Assert.Throws<InvalidDataException>(() => new DxtImage(data));
        }

        [Test]
        public void ValidMipLayoutDecodesOnlyTheRequestedLevel()
        {
            byte[] data = CreateDds(2);
            for (int i = 128; i < 136; i++) data[i] = 255;
            var image = new DxtImage(data);
            Assert.That(image.MipCount, Is.EqualTo(2));
            Assert.That(image.GetOffset(1), Is.EqualTo(144));
            Assert.That(image.GetWidth(1), Is.EqualTo(2));
            Assert.That(image.GetHeight(1), Is.EqualTo(2));
            byte[] pixels = new byte[2 * 2 * 4];
            image.Decode(1, pixels);
            Assert.That(pixels[3], Is.Zero);
            Assert.That(image.GetPixel(0, 0).a, Is.EqualTo(255));
        }

        private static byte[] CreateDds(int mips)
        {
            byte[] data = new byte[128 + mips * 16];
            WriteInt(data, 0, 0x20534444);
            WriteInt(data, 4, 124);
            WriteInt(data, 12, 4);
            WriteInt(data, 16, 4);
            WriteInt(data, 28, mips);
            WriteInt(data, 76, 32);
            WriteInt(data, 80, 4);
            WriteInt(data, 84, 0x33545844); // DXT3
            return data;
        }

        private static void WriteInt(byte[] data, int offset, int value) =>
            Buffer.BlockCopy(BitConverter.GetBytes(value), 0, data, offset, 4);
    }
}
