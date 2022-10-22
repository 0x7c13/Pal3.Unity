// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

// Taken from: https://github.com/AnzhelikaO/FakeProvider/blob/version-1.4/FakeProvider/UnsafeBinaryReader.cs
// with some modifications

namespace Core.Utils
{
    using System;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using System.Text;
    using UnityEngine;

    public sealed unsafe class UnsafeBinaryReader : IDisposable
    {
        private byte* _dataPtr;
        private GCHandle _handle;

        public UnsafeBinaryReader(byte[] data)
        {
            _handle = GCHandle.Alloc(data, GCHandleType.Pinned);
            fixed (byte* ptr = &data[0])
            {
                _dataPtr = ptr;
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
        public Vector2 ReadVector2()
        {
            return new Vector2(ReadSingle(), ReadSingle());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector3 ReadVector3()
        {
            return new Vector3(ReadSingle(), ReadSingle(), ReadSingle());
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
        public float[] ReadSingleArray(int count)
        {
            var value = new float[count];
            Marshal.Copy(new IntPtr(_dataPtr), value, 0, count);
            _dataPtr += count * 4;
            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public char[] ReadChars(int count)
        {
            return Encoding.ASCII.GetString(ReadBytes(count)).ToCharArray();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string ReadString(int count, int codepage)
        {
            var strBytes = ReadBytes(count);
            var i = 0;
            var length = strBytes.Length;
            while (i < length && strBytes[i] != 0) i++;
            return Encoding.GetEncoding(codepage).GetString(strBytes, 0, i);
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
