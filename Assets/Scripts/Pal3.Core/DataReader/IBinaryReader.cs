// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2025, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Core.DataReader
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
        public long Position { get; }
        public long Length { get; }
        public void Seek(long offset, SeekOrigin seekOrigin);
        public short ReadInt16();
        public int ReadInt32();
        public long ReadInt64();
        public ushort ReadUInt16();
        public uint ReadUInt32();
        public ulong ReadUInt64();
        public float ReadSingle();
        public double ReadDouble();
        public bool ReadBoolean();
        public byte ReadByte();
        public byte[] ReadBytes(int count);

        #region Helper Extensions (Default Implementation)
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float[] ReadSingles(int count)
        {
            var array = new float[count];
            for (var i = 0; i < count; i++)
            {
                array[i] = ReadSingle();
            }
            return array;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double[] ReadDoubles(int count)
        {
            var array = new double[count];
            for (var i = 0; i < count; i++)
            {
                array[i] = ReadDouble();
            }
            return array;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public short[] ReadInt16s(int count)
        {
            var array = new short[count];
            for (var i = 0; i < count; i++)
            {
                array[i] = ReadInt16();
            }
            return array;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ushort[] ReadUInt16s(int count)
        {
            var array = new ushort[count];
            for (var i = 0; i < count; i++)
            {
                array[i] = ReadUInt16();
            }
            return array;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int[] ReadInt32s(int count)
        {
            var array = new int[count];
            for (var i = 0; i < count; i++)
            {
                array[i] = ReadInt32();
            }
            return array;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint[] ReadUInt32s(int count)
        {
            var array = new uint[count];
            for (var i = 0; i < count; i++)
            {
                array[i] = ReadUInt32();
            }
            return array;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public long[] ReadInt64s(int count)
        {
            var array = new long[count];
            for (var i = 0; i < count; i++)
            {
                array[i] = ReadInt64();
            }
            return array;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ulong[] ReadUInt64s(int count)
        {
            var array = new ulong[count];
            for (var i = 0; i < count; i++)
            {
                array[i] = ReadUInt64();
            }
            return array;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T[] ReadEnums<T>(int count) where T : Enum
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
        public string ReadString(int count) => new (ReadChars(count));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string ReadString(int count, int codepage)
        {
            var strBytes = ReadBytes(count);
            int length = Array.IndexOf(strBytes, (byte)0);
            if (length == -1) length = strBytes.Length; // If no null byte is found, use the full length
            return Encoding.GetEncoding(codepage).GetString(strBytes, 0, length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public GameBoxVector2 ReadGameBoxVector2() => new (ReadSingle(), ReadSingle());

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public GameBoxVector2[] ReadGameBoxVector2s(int count)
        {
            var array = new GameBoxVector2[count];
            for (var i = 0; i < count; i++)
            {
                array[i] = ReadGameBoxVector2();
            }
            return array;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public GameBoxVector3 ReadGameBoxVector3() => new (ReadSingle(), ReadSingle(), ReadSingle());

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public GameBoxVector3[] ReadGameBoxVector3s(int count)
        {
            var array = new GameBoxVector3[count];
            for (var i = 0; i < count; i++)
            {
                array[i] = ReadGameBoxVector3();
            }
            return array;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Color32 ReadColor32() => new (ReadByte(), ReadByte(), ReadByte(), ReadByte());

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Color32[] ReadColor32s(int count)
        {
            var array = new Color32[count];
            for (var i = 0; i < count; i++)
            {
                array[i] = ReadColor32();
            }
            return array;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Color ReadColor() => new (ReadSingle(), ReadSingle(), ReadSingle(), ReadSingle());

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Color[] ReadColors(int count)
        {
            var array = new Color[count];
            for (var i = 0; i < count; i++)
            {
                array[i] = ReadColor();
            }
            return array;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public object Read(Type type)
        {
            return type switch
            {
                not null when type == typeof(bool) => ReadBoolean(),
                not null when type == typeof(byte) => ReadByte(),
                not null when type == typeof(short) => ReadInt16(),
                not null when type == typeof(ushort) => ReadUInt16(),
                not null when type == typeof(int) => ReadInt32(),
                not null when type == typeof(uint) => ReadUInt32(),
                not null when type == typeof(long) => ReadInt64(),
                not null when type == typeof(ulong) => ReadUInt64(),
                not null when type == typeof(float) => ReadSingle(),
                not null when type == typeof(double) => ReadDouble(),
                not null when type == typeof(GameBoxVector2) => ReadGameBoxVector2(),
                not null when type == typeof(GameBoxVector3) => ReadGameBoxVector3(),
                not null when type == typeof(Color32) => ReadColor32(),
                not null when type == typeof(Color) => ReadColor(),
                _ => throw new NotSupportedException($"Type {type} is not supported.")
            };
        }
        #endregion
    }
}