// ------------------------------------------------------------------------
//  Copyright (c) 2021-2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Core.DataReader
{
    using System;
    using System.IO;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using Primitives;

    /// <summary>
    /// Provides a binary reader that reads primitive data types from a byte array or stream using unsafe code.
    /// </summary>
    public sealed unsafe class UnsafeBinaryReader : IBinaryReader
    {
        private GCHandle _handle;
        private byte* _startPtr;
        private byte* _dataPtr;

        public UnsafeBinaryReader(byte[] data)
        {
            Init(data);
        }

        public UnsafeBinaryReader(Stream stream)
        {
            if (!stream.CanSeek)
            {
                throw new ArgumentException("Stream must be seekable", nameof(stream));
            }

            long position = stream.Position;
            byte[] data = new byte[checked((int)stream.Length)];
            stream.Seek(0, SeekOrigin.Begin);
            int offset = 0;
            while (offset < data.Length)
            {
                int read = stream.Read(data, offset, data.Length - offset);
                if (read == 0) throw new EndOfStreamException();
                offset += read;
            }
            Init(data);
            Seek(position, SeekOrigin.Begin);
        }

        private void Init(byte[] data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            Length = data.Length;
            if (Length == 0) return;

            _handle = GCHandle.Alloc(data, GCHandleType.Pinned);
            fixed (byte* ptr = data)
            {
                _startPtr = ptr;
                _dataPtr = ptr;
            }
        }

        public long Position
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _dataPtr - _startPtr;
        }

        public long Length { get; private set; }

        public void Seek(long offset, SeekOrigin seekOrigin)
        {
            if (_disposedValue) throw new ObjectDisposedException(nameof(UnsafeBinaryReader));
            long origin = seekOrigin switch
            {
                SeekOrigin.Begin => 0,
                SeekOrigin.Current => Position,
                SeekOrigin.End => Length,
                _ => throw new ArgumentException("Invalid SeekOrigin", nameof(seekOrigin))
            };
            long position = checked(origin + offset);
            if (position < 0) throw new IOException("Cannot seek before the beginning of the stream");
            _dataPtr = _startPtr + position;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EnsureAvailable(long count)
        {
            if (_disposedValue) throw new ObjectDisposedException(nameof(UnsafeBinaryReader));
            if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));
            if (count == 0) return;
            if (Position > Length || count > Length - Position) throw new EndOfStreamException();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public short ReadInt16()
        {
            EnsureAvailable(sizeof(short));
            short value = *(short*)_dataPtr;
            _dataPtr += sizeof(short);
            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int ReadInt32()
        {
            EnsureAvailable(sizeof(int));
            int value = *(int*)_dataPtr;
            _dataPtr += sizeof(int);
            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public long ReadInt64()
        {
            EnsureAvailable(sizeof(long));
            long value = *(long*)_dataPtr;
            _dataPtr += sizeof(long);
            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ushort ReadUInt16()
        {
            EnsureAvailable(sizeof(ushort));
            ushort value = *(ushort*)_dataPtr;
            _dataPtr += sizeof(ushort);
            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint ReadUInt32()
        {
            EnsureAvailable(sizeof(uint));
            uint value = *(uint*)_dataPtr;
            _dataPtr += sizeof(uint);
            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ulong ReadUInt64()
        {
            EnsureAvailable(sizeof(ulong));
            ulong value = *(ulong*)_dataPtr;
            _dataPtr += sizeof(ulong);
            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float ReadSingle()
        {
            EnsureAvailable(sizeof(float));
            float value = *(float*)_dataPtr;
            _dataPtr += sizeof(float);
            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double ReadDouble()
        {
            EnsureAvailable(sizeof(double));
            double value = *(double*)_dataPtr;
            _dataPtr += sizeof(double);
            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ReadBoolean() => ReadByte() != 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte ReadByte()
        {
            EnsureAvailable(sizeof(byte));
            byte value = *_dataPtr;
            _dataPtr += sizeof(byte);
            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte[] ReadBytes(int count)
        {
            if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));
            EnsureAvailable(0);
            count = (int)Math.Min(count, Math.Max(0, Length - Position));
            byte[] values = new byte[count];
            fixed (byte* dst = values)
            {
                Buffer.MemoryCopy(_dataPtr,
                    dst,
                    count,
                    count);
            }
            _dataPtr += count;
            return values;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float[] ReadSingles(int count)
        {
            EnsureAvailable((long)count * sizeof(float));
            float[] values = new float[count];
            fixed (float* dst = values)
            {
                Buffer.MemoryCopy(_dataPtr,
                    dst,
                    count * sizeof(float),
                    count * sizeof(float));
            }
            _dataPtr += count * sizeof(float);
            return values;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double[] ReadDoubles(int count)
        {
            EnsureAvailable((long)count * sizeof(double));
            double[] values = new double[count];
            fixed (double* dst = values)
            {
                Buffer.MemoryCopy(_dataPtr,
                    dst,
                    count * sizeof(double),
                    count * sizeof(double));
            }
            _dataPtr += count * sizeof(double);
            return values;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public short[] ReadInt16s(int count)
        {
            EnsureAvailable((long)count * sizeof(short));
            short[] values = new short[count];
            fixed (short* dst = values)
            {
                Buffer.MemoryCopy(_dataPtr,
                    dst,
                    count * sizeof(short),
                    count * sizeof(short));
            }
            _dataPtr += count * sizeof(short);
            return values;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int[] ReadInt32s(int count)
        {
            EnsureAvailable((long)count * sizeof(int));
            int[] values = new int[count];
            fixed (int* dst = values)
            {
                Buffer.MemoryCopy(_dataPtr,
                    dst,
                    count * sizeof(int),
                    count * sizeof(int));
            }
            _dataPtr += count * sizeof(int);
            return values;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public long[] ReadInt64s(int count)
        {
            EnsureAvailable((long)count * sizeof(long));
            long[] values = new long[count];
            fixed (long* dst = values)
            {
                Buffer.MemoryCopy(_dataPtr,
                    dst,
                    count * sizeof(long),
                    count * sizeof(long));
            }
            _dataPtr += count * sizeof(long);
            return values;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public GameBoxVector2 ReadGameBoxVector2()
        {
            EnsureAvailable(sizeof(GameBoxVector2));
            GameBoxVector2 value = *(GameBoxVector2*)_dataPtr;
            _dataPtr += sizeof(GameBoxVector2);
            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public GameBoxVector2[] ReadGameBoxVector2s(int count)
        {
            EnsureAvailable((long)count * sizeof(GameBoxVector2));
            GameBoxVector2[] values = new GameBoxVector2[count];
            fixed (GameBoxVector2* ptr = values)
            {
                Buffer.MemoryCopy(_dataPtr,
                    ptr,
                    count * sizeof(GameBoxVector2),
                    count * sizeof(GameBoxVector2));
            }
            _dataPtr += count * sizeof(GameBoxVector2);
            return values;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public GameBoxVector3 ReadGameBoxVector3()
        {
            EnsureAvailable(sizeof(GameBoxVector3));
            GameBoxVector3 value = *(GameBoxVector3*)_dataPtr;
            _dataPtr += sizeof(GameBoxVector3);
            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public GameBoxVector3[] ReadGameBoxVector3s(int count)
        {
            EnsureAvailable((long)count * sizeof(GameBoxVector3));
            GameBoxVector3[] values = new GameBoxVector3[count];
            fixed (GameBoxVector3* ptr = values)
            {
                Buffer.MemoryCopy(_dataPtr,
                    ptr,
                    count * sizeof(GameBoxVector3),
                    count * sizeof(GameBoxVector3));
            }
            _dataPtr += count * sizeof(GameBoxVector3);
            return values;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Color32 ReadColor32()
        {
            EnsureAvailable(sizeof(Color32));
            Color32 value = *(Color32*)_dataPtr;
            _dataPtr += sizeof(Color32);
            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Color32[] ReadColor32s(int count)
        {
            EnsureAvailable((long)count * sizeof(Color32));
            Color32[] values = new Color32[count];
            fixed (Color32* ptr = values)
            {
                Buffer.MemoryCopy(_dataPtr,
                    ptr,
                    count * sizeof(Color32),
                    count * sizeof(Color32));
            }
            _dataPtr += count * sizeof(Color32);
            return values;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Color ReadColor()
        {
            EnsureAvailable(sizeof(Color));
            Color value = *(Color*)_dataPtr;
            _dataPtr += sizeof(Color);
            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Color[] ReadColors(int count)
        {
            EnsureAvailable((long)count * sizeof(Color));
            Color[] values = new Color[count];
            fixed (Color* ptr = values)
            {
                Buffer.MemoryCopy(_dataPtr,
                    ptr,
                    count * sizeof(Color),
                    count * sizeof(Color));
            }
            _dataPtr += count * sizeof(Color);
            return values;
        }

        private bool _disposedValue;
        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    GC.SuppressFinalize(this);
                }

                if (_handle.IsAllocated) _handle.Free();
                _startPtr = null;
                _dataPtr = null;

                _disposedValue = true;
            }
        }

        ~UnsafeBinaryReader()
        {
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
        }
    }
}
