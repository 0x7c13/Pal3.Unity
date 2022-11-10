// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Core.Extensions
{
    using System.IO;
    using System.Text;
    using UnityEngine;

    /// <summary>
    /// BinaryReader extensions
    /// </summary>
    public static class BinaryReaderExtension
    {
        public static Vector3 ReadVector3(this BinaryReader binaryReader)
        {
            var x = binaryReader.ReadSingle();
            var y = binaryReader.ReadSingle();
            var z = binaryReader.ReadSingle();
            return new Vector3(x, y, z);
        }

        public static Vector3[] ReadVector3Array(this BinaryReader binaryReader, int count)
        {
            var v3List = new Vector3[count];
            for (var i = 0; i < count; i++)
            {
                v3List[i] = binaryReader.ReadVector3();
            }
            return v3List;
        }

        public static Vector2 ReadVector2(this BinaryReader binaryReader)
        {
            var x = binaryReader.ReadSingle();
            var y = binaryReader.ReadSingle();
            return new Vector2(x, y);
        }

        public static float[] ReadSingleArray(this BinaryReader binaryReader, int count)
        {
            var singles = new float[count];
            for (var i = 0; i < count; i++)
            {
                singles[i] = binaryReader.ReadSingle();
            }
            return singles;
        }

        public static int[] ReadInt32Array(this BinaryReader binaryReader, int count)
        {
            var integers = new int[count];
            for (var i = 0; i < count; i++)
            {
                integers[i] = binaryReader.ReadInt32();
            }
            return integers;
        }

        public static uint[] ReadUInt32Array(this BinaryReader binaryReader, int count)
        {
            var integers = new uint[count];
            for (var i = 0; i < count; i++)
            {
                integers[i] = binaryReader.ReadUInt32();
            }
            return integers;
        }
        
        public static short[] ReadInt16Array(this BinaryReader binaryReader, int count)
        {
            var integers = new short[count];
            for (var i = 0; i < count; i++)
            {
                integers[i] = binaryReader.ReadInt16();
            }
            return integers;
        }
        
        public static ushort[] ReadUInt16Array(this BinaryReader binaryReader, int count)
        {
            var integers = new ushort[count];
            for (var i = 0; i < count; i++)
            {
                integers[i] = binaryReader.ReadUInt16();
            }
            return integers;
        }

        public static string ReadAsciiString(this BinaryReader binaryReader, int count)
        {
            return new string(binaryReader.ReadChars(count));
        }

        public static string ReadString(this BinaryReader binaryReader, int count, int codepage)
        {
            var strBytes = binaryReader.ReadBytes(count);
            var i = 0;
            var length = strBytes.Length;
            while (i < length && strBytes[i] != 0) i++;
            return Encoding.GetEncoding(codepage).GetString(strBytes, 0, i);
        }
    }
}