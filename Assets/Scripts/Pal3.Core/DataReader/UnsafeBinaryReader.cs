// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2024, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

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

            Length = stream.Length;
            if (Length == 0) return;

            var data = new byte[Length];
            _ = stream.Read(data, 0, (int)Length);

            Init(data);
        }

        private void Init(byte[] data)
        {
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
            get => Length == 0 ? 0 : _dataPtr - _startPtr;
        }

        public long Length { get; private set; }

        public void Seek(long offset, SeekOrigin seekOrigin)
        {
            switch (seekOrigin)
            {
                case SeekOrigin.Begin:
                    _dataPtr = _startPtr + offset;
                    break;
                case SeekOrigin.Current:
                    _dataPtr += offset;
                    break;
                case SeekOrigin.End:
                    throw new NotSupportedException("SeekOrigin.End is not supported for UnsafeBinaryReader");
                default:
                    throw new ArgumentException("Invalid SeekOrigin", nameof(seekOrigin));
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public short ReadInt16()
        {
            short value = *(short*)_dataPtr;
            _dataPtr += sizeof(short);
            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int ReadInt32()
        {
            int value = *(int*)_dataPtr;
            _dataPtr += sizeof(int);
            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public long ReadInt64()
        {
            long value = *(long*)_dataPtr;
            _dataPtr += sizeof(long);
            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ushort ReadUInt16()
        {
            ushort value = *(ushort*)_dataPtr;
            _dataPtr += sizeof(ushort);
            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint ReadUInt32()
        {
            uint value = *(uint*)_dataPtr;
            _dataPtr += sizeof(uint);
            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ulong ReadUInt64()
        {
            ulong value = *(ulong*)_dataPtr;
            _dataPtr += sizeof(ulong);
            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float ReadSingle()
        {
            float value = *(float*)_dataPtr;
            _dataPtr += sizeof(float);
            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double ReadDouble()
        {
            double value = *(double*)_dataPtr;
            _dataPtr += sizeof(double);
            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ReadBoolean()
        {
            bool value = *(bool*)_dataPtr;
            _dataPtr += sizeof(bool);
            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte ReadByte()
        {
            byte value = *_dataPtr;
            _dataPtr += sizeof(byte);
            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte[] ReadBytes(int count)
        {
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
            GameBoxVector2 value = *(GameBoxVector2*)_dataPtr;
            _dataPtr += sizeof(GameBoxVector2);
            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public GameBoxVector2[] ReadGameBoxVector2s(int count)
        {
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
            GameBoxVector3 value = *(GameBoxVector3*)_dataPtr;
            _dataPtr += sizeof(GameBoxVector3);
            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public GameBoxVector3[] ReadGameBoxVector3s(int count)
        {
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
            Color32 value = *(Color32*)_dataPtr;
            _dataPtr += sizeof(Color32);
            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Color32[] ReadColor32s(int count)
        {
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
            Color value = *(Color*)_dataPtr;
            _dataPtr += sizeof(Color);
            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Color[] ReadColors(int count)
        {
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

                if (Length != 0)
                {
                    _handle.Free();
                }

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
