// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Core.DataReader
{
    /// <summary>
    /// File reader interface for reading data from binary file
    /// using <see cref="IBinaryReader"/>.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IFileReader<out T>
    {
        T Read(IBinaryReader reader);

        T Read(byte[] data)
        {
            // Use unsafe binary reader (faster) if IL2CPP is enabled
            // otherwise use safe reader
            #if ENABLE_IL2CPP
            using var reader = new UnsafeBinaryReader(data);
            #else
            using var reader = new SafeBinaryReader(data);
            #endif

            return Read(reader);
        }
    }
}