// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2024, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Core.FileSystem
{
    public enum FileSystemMode
    {
        ArchiveOnly,    // only read from archives
        DiskAndArchive, // read from disk first, then archives
    }
}