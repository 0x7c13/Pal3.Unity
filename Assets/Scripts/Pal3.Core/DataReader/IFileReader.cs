// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Core.DataReader
{
    using System.IO;

    /// <summary>
    /// File reader interface for reading data from binary file
    /// using <see cref="IBinaryReader"/>.
    /// </summary>
    /// <typeparam name="T">File type</typeparam>
    public interface IFileReader<out T>
    {
        T Read(IBinaryReader reader, int codepage);

        T Read(byte[] data, int codepage)
        {
            // Use unsafe binary reader (faster) if IL2CPP is enabled
            // otherwise use safe reader
            #if ENABLE_IL2CPP || UNITY_EDITOR
            using var reader = new UnsafeBinaryReader(data);
            #else
            using var reader = new SafeBinaryReader(data);
            #endif

            return Read(reader, codepage);
        }

        T Read(Stream stream, int codepage)
        {
            // Use unsafe binary reader (faster) if IL2CPP is enabled
            // otherwise use safe reader
            #if ENABLE_IL2CPP || UNITY_EDITOR
            using var reader = new UnsafeBinaryReader(stream);
            #else
            using var reader = new SafeBinaryReader(stream);
            #endif

            return Read(reader, codepage);
        }
    }
}