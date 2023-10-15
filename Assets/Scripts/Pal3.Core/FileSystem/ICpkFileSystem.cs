// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Core.FileSystem
{
    using System.Collections.Generic;

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
        /// <param name="codepage">Codepage CPK file uses for encoding text info</param>
        public void Mount(string cpkFileRelativePath, int codepage);

        /// <summary>
        /// Check if file exists in any of the segmented archives using virtual path.
        /// </summary>
        /// <param name="fileVirtualPath">File virtual path {Cpk file name}\{File relative path inside archive}</param>
        /// <param name="segmentedArchiveName">Name of the segmented archive if exists</param>
        /// <returns>True if file exists in segmented archive</returns>
        public bool FileExistsInSegmentedArchive(string fileVirtualPath, out string segmentedArchiveName);

        /// <summary>
        /// Check if file exists in the archive using virtual path.
        /// </summary>
        /// <param name="fileVirtualPath">File virtual path {Cpk file name}\{File relative path inside archive}
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
        /// Extract all archives to the specified destination
        /// </summary>
        public void ExtractTo(string outputFolder);

        /// <summary>
        /// Searches for files in the file system that match the specified keyword.
        /// </summary>
        /// <param name="keyword">The keyword to search for. If empty, all files will be returned.</param>
        /// <returns>A list of file paths that match the specified keyword.</returns>
        public IList<string> Search(string keyword = "");

        /// <summary>
        /// Searches for files in the CPK file system that match the specified keywords.
        /// </summary>
        /// <param name="keywords">The list of keywords to search for.</param>
        /// <returns>A dictionary where the keys are the file paths of the matching files,
        /// and the values are the lines in the files that contain the keywords.</returns>
        public IDictionary<string, IList<string>> BatchSearch(IList<string> keywords);
    }
}