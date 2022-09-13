// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Core.FileSystem
{
    using System.Collections.Generic;
    using System.IO;

    /// <summary>
    /// File system wrapper for CPack archives
    /// </summary>
    public interface ICpkFileSystem
    {
        /// <summary>
        /// Get file system's root path.
        /// </summary>
        /// <returns>Root directory path</returns>
        public string GetRootPath();

        /// <summary>
        /// Mount a Cpk archive to the file system.
        /// </summary>
        /// <param name="cpkFileRelativePath">CPK file relative path</param>
        public void Mount(string cpkFileRelativePath);

        /// <summary>
        /// Check if file exists in any of the segmented archives using virtual path.
        /// </summary>
        /// <param name="fileVirtualPath">File virtual path {Cpk file name}.cpk\{File relative path inside archive}</param>
        /// <param name="segmentedArchiveName">Name of the segmented archive if exists</param>
        /// <returns>True if file exists in segmented archive</returns>
        public bool FileExistsInSegmentedArchive(string fileVirtualPath, out string segmentedArchiveName);
        
        /// <summary>
        /// Check if file exists in the archive using virtual path.
        /// </summary>
        /// <param name="fileVirtualPath">File virtual path {Cpk file name}.cpk\{File relative path inside archive}
        /// Example: music.cpk\music\PI01.mp3</param>
        /// <returns>True if file exists</returns>
        public bool FileExists(string fileVirtualPath);

        /// <summary>
        /// Read all bytes of the given file.
        /// </summary>
        /// <param name="fileVirtualPath">File virtual path inside CPK archive</param>
        /// <returns>Decompressed, ready-to-go file content in byte array</returns>
        public byte[] ReadAllBytes(string fileVirtualPath);

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
        /// Search files using keyword.
        /// </summary>
        /// <param name="keyword"></param>
        /// <returns>File path enumerable</returns>
        public IEnumerable<string> Search(string keyword = "");
    }
}