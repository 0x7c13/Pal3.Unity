// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Core.Tests
{
    using DataReader.Tga;
    using NUnit.Framework;

    public sealed class TgaAlphaTests
    {
        [Test]
        public void RgbTgaExpandsToOpaqueAlphaIncludingTheCallerBufferPath()
        {
            var bytes = new byte[24];
            bytes[18] = 30;
            bytes[19] = 20;
            bytes[20] = 10;
            bytes[21] = 60;
            bytes[22] = 50;
            bytes[23] = 40;
            var expected = new byte[] { 10, 20, 30, 255, 40, 50, 60, 255 };
            Assert.That(TgaDecoder.Decode24BitDataToRgba32(bytes, 2, 1), Is.EqualTo(expected));
            var buffer = new byte[12];
            buffer[8] = 99;
            TgaDecoder.Decode24BitDataToRgba32(bytes, 2, 1, buffer);
            Assert.That(buffer[..8], Is.EqualTo(expected));
            Assert.That(buffer[8], Is.EqualTo(99));
        }

        [Test]
        public void RgbaTgaPreservesItsAuthoredAlpha()
        {
            var bytes = new byte[26];
            bytes[18] = 30;
            bytes[19] = 20;
            bytes[20] = 10;
            bytes[21] = 0;
            bytes[22] = 60;
            bytes[23] = 50;
            bytes[24] = 40;
            bytes[25] = 100;
            var rgba = TgaDecoder.Decode32BitDataToRgba32(bytes, 2, 1, out bool alpha);
            Assert.That(rgba, Is.EqualTo(new byte[] { 10, 20, 30, 0, 40, 50, 60, 100 }));
            Assert.That(alpha, Is.True);
        }
    }
}
