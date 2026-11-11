// See LICENSE in the project root.

namespace Pal3.Core.Tests
{
    using DataReader.Dxt;
    using NUnit.Framework;

    public sealed class Dxt3DecoderTests
    {
        [Test]
        public void FullyOpaqueBlockHasNoAlternatingTransparency()
        {
            byte[] pixels = Dxt3Decoder.ToRgba32(CreateDds(ulong.MaxValue), 4, 4);
            for (int pixel = 0; pixel < 16; pixel++)
            {
                Assert.That(pixels[pixel * 4 + 3], Is.EqualTo(255), $"Pixel {pixel} alpha");
                Assert.That(pixels[pixel * 4], Is.EqualTo(255), $"Pixel {pixel} red");
                Assert.That(pixels[pixel * 4 + 1], Is.Zero, $"Pixel {pixel} green");
                Assert.That(pixels[pixel * 4 + 2], Is.Zero, $"Pixel {pixel} blue");
            }
        }

        [Test]
        public void AlphaRampPreservesAllSixteenLevelsInPixelOrder()
        {
            byte[] pixels = Dxt3Decoder.ToRgba32(CreateDds(0xFEDCBA9876543210UL), 4, 4);
            byte[] expected = { 0, 17, 34, 51, 68, 85, 102, 119, 136, 153, 170, 187, 204, 221, 238, 255 };
            for (int pixel = 0; pixel < expected.Length; pixel++)
                Assert.That(pixels[pixel * 4 + 3], Is.EqualTo(expected[pixel]), $"Pixel {pixel} alpha");
        }

        [Test]
        public void PartialEdgeBlockKeepsItsOwnAlphaAndLeavesBufferTailUntouched()
        {
            byte[] pixels = new byte[5 * 3 * 4 + 4];
            for (int i = 5 * 3 * 4; i < pixels.Length; i++) pixels[i] = 123;
            Dxt3Decoder.ToRgba32(CreateDds(0xAAAAAAAAAAAAAAAAUL, ulong.MaxValue), 5, 3, pixels);
            for (int y = 0; y < 3; y++)
            for (int x = 0; x < 5; x++)
                Assert.That(pixels[(y * 5 + x) * 4 + 3], Is.EqualTo(x < 4 ? 170 : 255), $"Pixel {x},{y} alpha");
            for (int i = 5 * 3 * 4; i < pixels.Length; i++) Assert.That(pixels[i], Is.EqualTo(123));
        }

        private static byte[] CreateDds(params ulong[] alphaBlocks)
        {
            // The block decoder skips the 128-byte DDS header. Every color index selects red RGB565.
            byte[] data = new byte[128 + alphaBlocks.Length * 16];
            for (int block = 0; block < alphaBlocks.Length; block++)
            {
                int start = 128 + block * 16;
                for (int i = 0; i < 8; i++) data[start + i] = (byte)(alphaBlocks[block] >> (i * 8));
                data[start + 9] = 0xF8;
            }
            return data;
        }
    }
}
