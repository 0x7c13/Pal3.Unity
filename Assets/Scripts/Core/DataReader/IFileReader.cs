// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Core.DataReader
{
    using System.IO;

    /// <summary>
    /// File reader interface.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IFileReader<out T>
    {
        T Read(byte[] data)
        {
            using var stream = new MemoryStream(data);
            return Read(stream);
        }

        T Read(Stream stream)
        {
            using var memoryStream = new MemoryStream();
            stream.CopyTo(memoryStream);
            return Read(memoryStream.ToArray());
        }
    }
}