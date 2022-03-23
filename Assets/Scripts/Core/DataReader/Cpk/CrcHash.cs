// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Core.DataReader.Cpk
{
    using System.Linq;
    using System.Text;

    public class CrcHash
    {
        private const uint CRC_TABLE_MAX = 256;
        private const uint POLYNOMIAL = 0x04C11DB7; // CRC seed
        private const int GBK_CODE_PAGE = 936; // GBK Encoding's code page

        private static readonly uint[] CrcTable = new uint[CRC_TABLE_MAX];

        public CrcHash()
        {
            // generate the table of CRC remainders for all possible bytes
            for (uint i = 0; i < CRC_TABLE_MAX; i++)
            {
                var crcAccum = i << 24;

                for (var j = 0; j < 8; j++)
                {
                    if ((crcAccum & 0x80000000L) != 0)
                    {
                        crcAccum = (crcAccum << 1) ^ POLYNOMIAL;
                    }
                    else
                    {
                        crcAccum = (crcAccum << 1);
                    }
                }
                CrcTable[i] = crcAccum;
            }
        }

        public uint ComputeCrc32Hash(string str)
        {
            //Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            return ComputeCrc32Hash(Encoding.GetEncoding(GBK_CODE_PAGE).GetBytes(str));
        }

        public unsafe uint ComputeCrc32Hash(byte[] data)
        {
            var length = data.Length;
            if (length == 0 || data[0] == 0) return 0;

            var index = 0;
            fixed (byte* srcStart = &data[0])
            {
                var p = srcStart;
                uint result  = (uint)(*(p + index++) << 24);
                if (index < length && *(p + index) != 0)
                {
                    result |= (uint)(*(p + index++) << 16);

                    if(index < length && *(p + index) != 0)
                    {
                        result |= (uint)(*(p + index++) << 8);
                        if (index < length && *(p + index) != 0)
                        {
                            result |= *(p + index++);
                        }
                    }
                }
                result = ~result;

                while (index < length && *(p + index) != 0)
                {
                    result = (result << 8 | *(p + index)) ^ CrcTable[result >> 24];
                    index++;
                }

                return ~result;
            }
        }
    }
}