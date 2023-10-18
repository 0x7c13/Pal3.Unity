// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Core.DataReader
{
    using System;
    using System.IO;

    /// <summary>
    /// Provides a safe way to read binary data from a stream or byte array.
    /// </summary>
    public sealed class SafeBinaryReader : IBinaryReader
    {
        private readonly BinaryReader _reader;

        private SafeBinaryReader() { }

        public SafeBinaryReader(byte[] data) : this (new MemoryStream(data)) { }

        public SafeBinaryReader(Stream stream)
        {
            _reader = new BinaryReader(stream);
        }

        public void Seek(long offset, SeekOrigin seekOrigin)
        {
            _reader.BaseStream.Seek(offset, seekOrigin);
        }

        public long Position => _reader.BaseStream.Position;
        public long Length => _reader.BaseStream.Length;
        public Stream BaseStream => _reader.BaseStream;

        public short ReadInt16() => _reader.ReadInt16();
        public int ReadInt32() => _reader.ReadInt32();
        public long ReadInt64() => _reader.ReadInt64();
        public ushort ReadUInt16() => _reader.ReadUInt16();
        public uint ReadUInt32() => _reader.ReadUInt32();
        public ulong ReadUInt64() => _reader.ReadUInt64();
        public float ReadSingle() => _reader.ReadSingle();
        public double ReadDouble() => _reader.ReadDouble();
        public bool ReadBoolean() => _reader.ReadBoolean();
        public byte ReadByte() => _reader.ReadByte();
        public byte[] ReadBytes(int count) => _reader.ReadBytes(count);

        private bool _disposedValue;
        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    GC.SuppressFinalize(this);
                }
                _reader?.Dispose();
                _disposedValue = true;
            }
        }

        ~SafeBinaryReader()
        {
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
        }
    }
}
