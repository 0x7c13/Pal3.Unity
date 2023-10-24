// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Core.DataReader.Cpk
{
    using System;
    using System.Buffers;
    using System.Collections.Generic;
    using System.Text;

    /// <summary>
    /// Crc32 hash provider.
    /// </summary>
    public sealed class Crc32Hash
    {
        private const uint CRC_TABLE_MAX = 0xFF;
        private const uint POLYNOMIAL = 0x04C11DB7; // CRC seed

        private bool _initialized;
        private static readonly uint[] CrcTable = new uint[CRC_TABLE_MAX + 1];

        private readonly Dictionary<int, Encoding> _encodings = new ();

        public void Init()
        {
            if (_initialized) return;

            // Generate the table of CRC remainders for all possible bytes
            for (uint i = 0; i <= CRC_TABLE_MAX; i++)
            {
                uint crcAccum = i << 24;
                for (var j = 0; j < 8; j++)
                {
                    crcAccum = (crcAccum & 0x80000000L) != 0 ?
                        (crcAccum << 1) ^ POLYNOMIAL :
                        (crcAccum << 1);
                }
                CrcTable[i] = crcAccum;
            }

            _initialized = true;
        }

        public uint Compute(string str, int codepage)
        {
            if (!_initialized) throw new InvalidOperationException($"Crc32 hash table is not initialized yet");

            if (string.IsNullOrEmpty(str)) return 0;

            if (!_encodings.TryGetValue(codepage, out Encoding encoding))
            {
                encoding = Encoding.GetEncoding(codepage);
                _encodings.Add(codepage, encoding);
            }

            int byteCount = encoding.GetByteCount(str);

            byte[] rentedBuffer = null;
            Span<byte> buffer = byteCount <= 512  // To prevent stack overflow when string is too long
                ? stackalloc byte[byteCount]
                : (rentedBuffer = ArrayPool<byte>.Shared.Rent(byteCount)).AsSpan();

            try
            {
                encoding.GetBytes(chars: str.AsSpan(), buffer);
                return ComputeInternal(buffer[..byteCount]); // Use range operator here since rentedArray
                                                             // may be greater than byteCount
            }
            finally
            {
                if (rentedBuffer != null)
                {
                    ArrayPool<byte>.Shared.Return(rentedBuffer);
                }
            }
        }

        private unsafe uint ComputeInternal(Span<byte> data)
        {
            if (data == null || data.Length == 0 || data[0] == 0)
            {
                return 0;
            }

            fixed (byte* srcStart = data)
            {
                byte* ptr = srcStart;
                uint result = (uint)(*ptr++ << 24);

                for (int i = 1; i < 4 && i < data.Length; i++)
                {
                    if (*ptr == 0) break;
                    result |= (uint)(*ptr++ << (24 - 8 * i));
                }

                result = ~result;

                for (int i = 4; i < data.Length && *ptr != 0; i++, ptr++)
                {
                    result = (result << 8 | *ptr) ^ CrcTable[result >> 24];
                }

                return ~result;
            }
        }
    }
}