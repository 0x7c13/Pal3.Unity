// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2024, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Core.FileSystem
{
    using System.Collections.Generic;

    /// <summary>
    /// Interface for file system operations.
    /// </summary>
    public interface IFileSystem
    {
        /// <summary>
        /// Get file system's root path.
        /// </summary>
        /// <returns>Root directory path</returns>
        public string GetRootPath();

        /// <summary>
        /// Check if file exists in the archive or any of the segmented archives using virtual path.
        /// </summary>
        /// <param name="fileVirtualPath">File virtual path
        /// Example: music.cpk\music\PI01.mp3</param>
        /// <returns>True if file exists</returns>
        public bool FileExists(string fileVirtualPath);

        /// <summary>
        /// Read all bytes of the given file.
        /// </summary>
        /// <param name="fileVirtualPath">File virtual path</param>
        /// <returns>File content in byte array</returns>
        public byte[] ReadAllBytes(string fileVirtualPath);

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