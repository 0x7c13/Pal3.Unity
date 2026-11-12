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

    internal static class ProtocolTestData
    {
        internal static byte[] Build(Action<BinaryWriter> write)
        {
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);
            write(writer);
            return stream.ToArray();
        }

        internal static void FixedString(this BinaryWriter writer, string text, int length, int codepage = 936)
        {
            byte[] bytes = Encoding.GetEncoding(codepage).GetBytes(text);
            if (bytes.Length > length) throw new ArgumentException(nameof(text));
            writer.Write(bytes);
            writer.Write(new byte[length - bytes.Length]);
        }

        internal static void At(this BinaryWriter writer, long position)
        {
            while (writer.BaseStream.Length < position) writer.Write((byte)0xCD);
            writer.BaseStream.Position = position;
        }

        internal static IBinaryReader Reader(byte[] data, bool unsafeReader) =>
            unsafeReader ? new UnsafeBinaryReader(data) : new SafeBinaryReader(data);

        internal sealed class ShortReadStream : MemoryStream
        {
            internal ShortReadStream(byte[] data) : base(data) { }
            public override int Read(byte[] buffer, int offset, int count) => base.Read(buffer, offset, Math.Min(3, count));
            public override int Read(Span<byte> buffer) => base.Read(buffer[..Math.Min(3, buffer.Length)]);
        }
    }
}
