// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2024, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Core.FileSystem
{
    /// <summary>
    /// File system wrapper for CPack archives
    /// </summary>
    public interface ICpkFileSystem : IFileSystem
    {
        /// <summary>
        /// Mount a Cpk archive to the file system.
        /// </summary>
        /// <param name="cpkFileRelativePath">CPK file relative path</param>
        /// <param name="codepage">Codepage CPK file uses for encoding text info</param>
        public void Mount(string cpkFileRelativePath, int codepage);

        /// <summary>
        /// Check if file exists in the archive or any of the segmented archives using virtual path.
        /// </summary>
        /// <param name="fileVirtualPath">File virtual path {Cpk file name}\{File relative path inside archive}
        /// Example: music.cpk\music\PI01.mp3</param>
        /// <param name="archiveName">Name of the archive or the segmented archive if exists</param>
        /// <returns>True if file exists</returns>
        public bool FileExists(string fileVirtualPath, out string archiveName);

        /// <summary>
        /// Preload archive into memory for faster read performance.
        /// </summary>
        public void LoadArchiveIntoMemory(string cpkFileName);

        /// <summary>
        /// Dispose in-memory archive data.
        /// </summary>
        public void DisposeInMemoryArchive(string cpkFileName);

        /// <summary>
        /// Dispose all in-memory archive data.
        /// </summary>
        public void DisposeAllInMemoryArchives();

        /// <summary>
        /// Extract all archives to the specified destination
        /// </summary>
        public void ExtractTo(string outputFolder);
    }
}