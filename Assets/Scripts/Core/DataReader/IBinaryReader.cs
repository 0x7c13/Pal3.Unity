// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Core.DataReader
{
    using System;
    using System.IO;
    using System.Runtime.CompilerServices;
    using System.Text;
    using Primitives;

    /// <summary>
    /// Binary reader interface for reading data from binary file
    /// with extension methods for reading various data types.
    /// </summary>
    public interface IBinaryReader : IDisposable
    {
        long Position { get; }
        long Length { get; }
        void Seek(long offset, SeekOrigin seekOrigin);
        short ReadInt16();
        int ReadInt32();
        long ReadInt64();
        ushort ReadUInt16();
        uint ReadUInt32();
        ulong ReadUInt64();
        float ReadSingle();
        double ReadDouble();
        bool ReadBoolean();
        byte ReadByte();
        byte[] ReadBytes(int count);

        #region Helper Extensions (Default Implementation)
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        float[] ReadSingles(int count)
        {
            var array = new float[count];
            for (var i = 0; i < count; i++)
            {
                array[i] = ReadSingle();
            }
            return array;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        double[] ReadDoubles(int count)
        {
            var array = new double[count];
            for (var i = 0; i < count; i++)
            {
                array[i] = ReadDouble();
            }
            return array;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        short[] ReadInt16s(int count)
        {
            var array = new short[count];
            for (var i = 0; i < count; i++)
            {
                array[i] = ReadInt16();
            }
            return array;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        ushort[] ReadUInt16s(int count)
        {
            var array = new ushort[count];
            for (var i = 0; i < count; i++)
            {
                array[i] = ReadUInt16();
            }
            return array;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        int[] ReadInt32s(int count)
        {
            var array = new int[count];
            for (var i = 0; i < count; i++)
            {
                array[i] = ReadInt32();
            }
            return array;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        uint[] ReadUInt32s(int count)
        {
            var array = new uint[count];
            for (var i = 0; i < count; i++)
            {
                array[i] = ReadUInt32();
            }
            return array;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        long[] ReadInt64s(int count)
        {
            var array = new long[count];
            for (var i = 0; i < count; i++)
            {
                array[i] = ReadInt64();
            }
            return array;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        ulong[] ReadUInt64s(int count)
        {
            var array = new ulong[count];
            for (var i = 0; i < count; i++)
            {
                array[i] = ReadUInt64();
            }
            return array;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        T[] ReadEnums<T>(int count) where T : Enum
        {
            var array = new T[count];
            for (var i = 0; i < count; i++)
            {
                array[i] = (T)Enum.ToObject(typeof(T), ReadInt32());
            }
            return array;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public char[] ReadChars(int count)
        {
            return Encoding.ASCII.GetChars(ReadBytes(count));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        string ReadString(int count) => new (ReadChars(count));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        string ReadString(int count, int codepage)
        {
            var strBytes = ReadBytes(count);
            int length = Array.IndexOf(strBytes, (byte)0);
            if (length == -1) length = strBytes.Length; // If no null byte is found, use the full length
            return Encoding.GetEncoding(codepage).GetString(strBytes, 0, length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        GameBoxVector2 ReadVector2() => new (ReadSingle(), ReadSingle());

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        GameBoxVector3 ReadVector3() => new (ReadSingle(), ReadSingle(), ReadSingle());

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        GameBoxVector3[] ReadVector3s(int count)
        {
            var array = new GameBoxVector3[count];
            for (var i = 0; i < count; i++)
            {
                array[i] = ReadVector3();
            }
            return array;
        }
        #endregion
    }
}