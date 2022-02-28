// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE.txt in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Core.Extensions
{
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using UnityEngine;

    /// <summary>
    /// BinaryReader extensions
    /// </summary>
    public static class BinaryReaderExtension
    {
        private const int GBK_CODE_PAGE = 936; // GBK Encoding's code page

        public static Vector3 ReadVector3(this BinaryReader binaryReader)
        {
            var x = binaryReader.ReadSingle();
            var y = binaryReader.ReadSingle();
            var z = binaryReader.ReadSingle();
            return new Vector3(x, y, z);
        }

        public static Vector3[] ReadVector3Array(this BinaryReader binaryReader, int count)
        {
            var v3List = new List<Vector3>();
            for (var i = 0; i < count; i++)
            {
                v3List.Add(binaryReader.ReadVector3());
            }

            return v3List.ToArray();
        }

        public static Vector2 ReadVector2(this BinaryReader binaryReader)
        {
            var x = binaryReader.ReadSingle();
            var y = binaryReader.ReadSingle();
            return new Vector2(x, y);
        }

        public static float[] ReadSingleArray(this BinaryReader binaryReader, int count)
        {
            var singles = new List<float>();
            for (var i = 0; i < count; i++)
            {
                singles.Add(binaryReader.ReadSingle());
            }
            return singles.ToArray();
        }

        public static int[] ReadInt32Array(this BinaryReader binaryReader, int count)
        {
            var integers = new List<int>();
            for (var i = 0; i < count; i++)
            {
                integers.Add(binaryReader.ReadInt32());
            }
            return integers.ToArray();
        }

        public static uint[] ReadUInt32Array(this BinaryReader binaryReader, int count)
        {
            var integers = new List<uint>();
            for (var i = 0; i < count; i++)
            {
                integers.Add(binaryReader.ReadUInt32());
            }
            return integers.ToArray();
        }

        public static string ReadAsciiString(this BinaryReader binaryReader, int count)
        {
            return new string(binaryReader.ReadChars(count));
        }

        public static string ReadGbkString(this BinaryReader binaryReader, int count)
        {
            var strBytes = binaryReader.ReadBytes(count);
            var str = new List<byte>();
            var i = 0;
            while (i < strBytes.Length && strBytes[i] != 0) str.Add(strBytes[i++]);
            return Encoding.GetEncoding(GBK_CODE_PAGE).GetString(str.ToArray());
        }
    }
}