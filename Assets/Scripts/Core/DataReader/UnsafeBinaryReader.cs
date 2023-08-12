// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Core.DataReader
{
    using System;
    using System.IO;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;

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

            var length = stream.Length;
            var data = new byte[length];
            _ = stream.Read(data, 0, (int)length);

            Init(data);
        }

        private void Init(byte[] data)
        {
            Length = data.Length;
            _handle = GCHandle.Alloc(data, GCHandleType.Pinned);
            fixed (byte* ptr = &data[0])
            {
                _startPtr = ptr;
                _dataPtr = ptr;
            }
        }

        public long Position => _dataPtr - _startPtr;
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
            _dataPtr += 2;
            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int ReadInt32()
        {
            int value = *(int*)_dataPtr;
            _dataPtr += 4;
            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public long ReadInt64()
        {
            long value = *(long*)_dataPtr;
            _dataPtr += 8;
            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ushort ReadUInt16()
        {
            ushort value = *(ushort*)_dataPtr;
            _dataPtr += 2;
            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint ReadUInt32()
        {
            uint value = *(uint*)_dataPtr;
            _dataPtr += 4;
            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ulong ReadUInt64()
        {
            ulong value = *(ulong*)_dataPtr;
            _dataPtr += 8;
            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float ReadSingle()
        {
            float value = *(float*)_dataPtr;
            _dataPtr += 4;
            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double ReadDouble()
        {
            double value = *(double*)_dataPtr;
            _dataPtr += 8;
            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ReadBoolean()
        {
            bool value = *(bool*)_dataPtr;
            _dataPtr += 1;
            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte ReadByte()
        {
            byte value = *_dataPtr;
            _dataPtr += 1;
            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte[] ReadBytes(int count)
        {
            var value = new byte[count];
            Marshal.Copy(new IntPtr(_dataPtr), value, 0, count);
            _dataPtr += count;
            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float[] ReadSingles(int count)
        {
            var value = new float[count];
            Marshal.Copy(new IntPtr(_dataPtr), value, 0, count);
            _dataPtr += count * 4;
            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double[] ReadDoubles(int count)
        {
            var value = new double[count];
            Marshal.Copy(new IntPtr(_dataPtr), value, 0, count);
            _dataPtr += count * 8;
            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public short[] ReadInt16s(int count)
        {
            var value = new short[count];
            Marshal.Copy(new IntPtr(_dataPtr), value, 0, count);
            _dataPtr += count * 2;
            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int[] ReadInt32s(int count)
        {
            var value = new int[count];
            Marshal.Copy(new IntPtr(_dataPtr), value, 0, count);
            _dataPtr += count * 4;
            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public long[] ReadInt64s(int count)
        {
            var value = new long[count];
            Marshal.Copy(new IntPtr(_dataPtr), value, 0, count);
            _dataPtr += count * 8;
            return value;
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
                _handle.Free();
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
