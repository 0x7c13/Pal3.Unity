// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE.txt in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Core.FileSystem
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using DataReader.Cpk;
    using UnityEngine;

    /// <summary>
    /// File system wrapper for CPack archives
    /// </summary>
    public class CpkFileSystem : ICpkFileSystem
    {
        private readonly string _rootPath;
        private readonly ConcurrentDictionary<string, CpkArchive> _cpkArchives = new();

        private CpkFileSystem() {} // Hide default constructor

        public CpkFileSystem(string rootPath)
        {
            if (!rootPath.EndsWith(Path.DirectorySeparatorChar))
            {
                rootPath += Path.DirectorySeparatorChar;
            }

            if (!Directory.Exists(rootPath))
            {
                var error = "Failed to initialize CpkFileSystem" +
                            $" since application's root directory does not exists: {rootPath}";
                Debug.LogError(error);
                throw new ArgumentException(error);
            }

            _rootPath = rootPath;
        }

        public string GetRootPath()
        {
            return _rootPath;
        }

        /// <summary>
        /// Mount a Cpk archive to the file system.
        /// </summary>
        /// <param name="cpkFileRelativePath">CPK file relative path</param>
        public void Mount(string cpkFileRelativePath)
        {
            var cpkFileName = Path.GetFileName(cpkFileRelativePath).ToLower();

            if (_cpkArchives.ContainsKey(cpkFileName.ToLower()))
            {
                Debug.LogWarning($"{cpkFileRelativePath} already mounted.");
                return;
            }

            Debug.Log($"CpkFileSystem mounting: {_rootPath + cpkFileRelativePath}");
            var cpkArchive = new CpkArchive(_rootPath + cpkFileRelativePath);
            _cpkArchives[cpkFileName] = cpkArchive;
        }

        /// <summary>
        /// Check if file exists in the archive using virtual path.
        /// </summary>
        /// <param name="fileVirtualPath">File virtual path {Cpk file name}.cpk\{File relative path inside archive}
        /// Example: music.cpk\music\PI01.mp3</param>
        /// <returns>True if file exists</returns>
        public bool FileExists(string fileVirtualPath)
        {
            ParseFileVirtualPath(fileVirtualPath, out var cpkFileName, out var relativeVirtualPath);
            return _cpkArchives.Keys.Contains(cpkFileName.ToLower()) &&
                   _cpkArchives[cpkFileName].FileExists(relativeVirtualPath);
        }

        /// <summary>
        /// Open a stream pointing to the file position and return size
        /// of the file for caller to consume.
        /// </summary>
        /// <param name="fileVirtualPath">File virtual path inside CPK archive</param>
        /// <param name="size">Size of the file</param>
        /// <param name="compressed">True if file is compressed</param>
        /// <returns>Stream to consume</returns>
        public Stream Open(string fileVirtualPath, out uint size, out bool compressed)
        {
            if (!FileExists(fileVirtualPath))
            {
                throw new ArgumentException($"File: {fileVirtualPath} does not exists.");
            }

            ParseFileVirtualPath(fileVirtualPath, out var cpkFileName, out var relativeVirtualPath);
            return _cpkArchives[cpkFileName].Open(relativeVirtualPath, out size, out compressed);
        }

        /// <summary>
        /// Read all bytes of the given file
        /// </summary>
        /// <param name="fileVirtualPath">File virtual path inside CPK archive</param>
        /// <returns>Decompressed, ready-to-go file content in byte array</returns>
        public byte[] ReadAllBytes(string fileVirtualPath)
        {
            if (!FileExists(fileVirtualPath))
            {
                throw new ArgumentException($"File: {fileVirtualPath} does not exists.");
            }

            ParseFileVirtualPath(fileVirtualPath, out var cpkFileName, out var relativeVirtualPath);
            return _cpkArchives[cpkFileName].ReadAllBytes(relativeVirtualPath);
        }

        /// <summary>
        /// Preload archive into memory for faster read performance
        /// </summary>
        public void LoadArchiveIntoMemory(string cpkFileName)
        {
            if (_cpkArchives.ContainsKey(cpkFileName.ToLower()))
            {
                Debug.Log($"File system caching {cpkFileName} into memory.");
                _cpkArchives[cpkFileName.ToLower()].LoadArchiveIntoMemory();
            }
            else
            {
                throw new Exception($"{cpkFileName} not mounted yet.");
            }
        }

        /// <summary>
        /// Dispose in-memory archive data
        /// </summary>
        public void DisposeInMemoryArchive(string cpkFileName)
        {
            if (_cpkArchives.ContainsKey(cpkFileName.ToLower()))
            {
                Debug.Log($"File system disposing in-memory cache: {cpkFileName}");
                _cpkArchives[cpkFileName.ToLower()].DisposeInMemoryArchive();
            }
            else
            {
                throw new Exception($"{cpkFileName} not mounted yet.");
            }
        }

        /// <summary>
        /// Dispose all in-memory archive data
        /// </summary>
        public void DisposeAllInMemoryArchives()
        {
            foreach (var cpkArchive in _cpkArchives.Values)
            {
                cpkArchive.DisposeInMemoryArchive();
            }
        }

        /// <summary>
        /// Search files using keyword
        /// </summary>
        /// <param name="keyword"></param>
        /// <returns>File path enumerable</returns>
        public IEnumerable<string> Search(string keyword = "")
        {
            return from archive in _cpkArchives
                let rootNodes = archive.Value.GetRootEntries()
                from result in SearchInternal(rootNodes, keyword)
                select archive.Key + CpkConstants.CpkDirectorySeparatorChar + result;
        }

        private void ParseFileVirtualPath(string fullVirtualPath, out string cpkFileName, out string relativeVirtualPath)
        {
            if (!fullVirtualPath.Contains(CpkConstants.CpkDirectorySeparatorChar))
            {
                throw new ArgumentException("File virtual path is invalid.");
            }
            cpkFileName = fullVirtualPath[..fullVirtualPath.IndexOf(CpkConstants.CpkDirectorySeparatorChar)].ToLower();
            relativeVirtualPath = fullVirtualPath[(fullVirtualPath.IndexOf(CpkConstants.CpkDirectorySeparatorChar) + 1)..];
        }

        private IEnumerable<string> SearchInternal(IEnumerable<CpkEntry> nodes, string keyword)
        {
            foreach (var node in nodes)
            {
                if (node.IsDirectory)
                {
                    foreach (var result in SearchInternal(node.Children, keyword))
                    {
                        yield return result;
                    }
                }
                else if (node.VirtualPath.ToLower().Contains(keyword.ToLower()))
                {
                    yield return node.VirtualPath;
                }
            }
        }
    }
}