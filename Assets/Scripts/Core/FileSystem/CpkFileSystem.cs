// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Core.FileSystem
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using DataReader.Cpk;
    using UnityEngine;
    using Utils;

    /// <summary>
    /// File system wrapper for CPack archives
    /// </summary>
    public class CpkFileSystem : ICpkFileSystem
    {
        private readonly string _rootPath;
        private readonly ConcurrentDictionary<string, CpkArchive> _cpkArchives = new();
        private readonly CrcHash _crcHash;

        private CpkFileSystem() {} // Hide default constructor

        public CpkFileSystem(string rootPath, CrcHash crcHash)
        {
            rootPath = Requires.IsNotNullOrEmpty(rootPath, nameof(rootPath));
            crcHash = Requires.IsNotNull(crcHash, nameof(crcHash));

            if (!rootPath.EndsWith(Path.DirectorySeparatorChar))
            {
                rootPath += Path.DirectorySeparatorChar;
            }

            if (!Directory.Exists(rootPath))
            {
                var error = $"游戏数据加载失败，原始游戏数据根目录不存在: {rootPath}";
                Debug.LogError(error);
                throw new ArgumentException(error);
            }

            _rootPath = rootPath;
            _crcHash = crcHash;
        }

        public string GetRootPath()
        {
            return _rootPath;
        }

        /// <summary>
        /// Mount a Cpk archive to the file system.
        /// </summary>
        /// <param name="cpkFileRelativePath">CPK file relative path</param>
        /// <param name="codepage">Codepage CPK file uses for encoding text info</param>
        public void Mount(string cpkFileRelativePath, int codepage)
        {
            var cpkFileName = Path.GetFileName(cpkFileRelativePath).ToLower();

            if (_cpkArchives.ContainsKey(cpkFileName))
            {
                Debug.LogWarning($"{cpkFileRelativePath} already mounted.");
                return;
            }

            Debug.Log($"CpkFileSystem mounting: {_rootPath + cpkFileRelativePath}");
            var cpkArchive = new CpkArchive(_rootPath + cpkFileRelativePath, _crcHash, codepage);
            cpkArchive.Init();
            _cpkArchives[cpkFileName] = cpkArchive;
        }

        /// <summary>
        /// Check if file exists in any of the segmented archives using virtual path.
        /// </summary>
        /// <param name="fileVirtualPath">File virtual path {Cpk file name}.cpk\{File relative path inside archive}</param>
        /// <param name="segmentedArchiveName">Name of the segmented archive if exists</param>
        /// <returns>True if file exists in segmented archive</returns>
        public bool FileExistsInSegmentedArchive(string fileVirtualPath, out string segmentedArchiveName)
        {
            ParseFileVirtualPath(fileVirtualPath, out var cpkFileName, out var relativeVirtualPath);

            if (_cpkArchives.ContainsKey(cpkFileName))
            {
                segmentedArchiveName = null;
                return false;
            }

            // Check if the file exists in any of the segmented cpk archives.
            foreach (var subCpkPath in _cpkArchives.Keys
                         .Where(_ => _.StartsWith(cpkFileName[..^CpkConstants.FileExtension.Length] + '_', StringComparison.OrdinalIgnoreCase)))
            {
                var relativePathInSubCpk = relativeVirtualPath.Replace(cpkFileName, subCpkPath);
                if (_cpkArchives[subCpkPath].FileExists(relativePathInSubCpk))
                {
                    segmentedArchiveName = subCpkPath;
                    return true;
                }
            }

            segmentedArchiveName = null;
            return false;
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

            if (_cpkArchives.ContainsKey(cpkFileName))
            {
                return _cpkArchives[cpkFileName].FileExists(relativeVirtualPath);
            }

            // Check if the file exists in any of the segmented cpk archives.
            foreach (var subCpkPath in _cpkArchives.Keys
                         .Where(_ => _.StartsWith(cpkFileName[..^CpkConstants.FileExtension.Length] + '_', StringComparison.OrdinalIgnoreCase)))
            {
                var relativePathInSubCpk = relativeVirtualPath.Replace(cpkFileName, subCpkPath);
                if (_cpkArchives[subCpkPath].FileExists(relativePathInSubCpk))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Read all bytes of the given file.
        /// </summary>
        /// <param name="fileVirtualPath">File virtual path inside CPK archive</param>
        /// <returns>Decompressed, ready-to-go file content in byte array</returns>
        public byte[] ReadAllBytes(string fileVirtualPath)
        {
            ParseFileVirtualPath(fileVirtualPath, out var cpkFileName, out var relativeVirtualPath);

            if (_cpkArchives.ContainsKey(cpkFileName))
            {
                return _cpkArchives[cpkFileName].ReadAllBytes(relativeVirtualPath);
            }

            // Find and read the file content in segmented cpk archives.
            foreach (var subCpkPath in _cpkArchives.Keys
                         .Where(_ => _.StartsWith(cpkFileName[..^CpkConstants.FileExtension.Length] + '_', StringComparison.OrdinalIgnoreCase)))
            {
                var relativePathInSubCpk = relativeVirtualPath.Replace(cpkFileName, subCpkPath);
                if (_cpkArchives[subCpkPath].FileExists(relativePathInSubCpk))
                {
                    return _cpkArchives[subCpkPath].ReadAllBytes(relativePathInSubCpk);
                }
            }

            throw new Exception($"{fileVirtualPath} not found.");
        }

        /// <summary>
        /// Preload archive into memory for faster read performance.
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
        /// Dispose in-memory archive data.
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
        /// Dispose all in-memory archive data.
        /// </summary>
        public void DisposeAllInMemoryArchives()
        {
            foreach (CpkArchive cpkArchive in _cpkArchives.Values)
            {
                cpkArchive.DisposeInMemoryArchive();
            }
        }

        /// <summary>
        /// Extract all archives to the specified destination
        /// </summary>
        public void ExtractTo(string outputFolder)
        {
            foreach ((string cpkFileName, CpkArchive cpkArchive) in _cpkArchives)
            {
                var outputDir = outputFolder + cpkFileName + Path.DirectorySeparatorChar;

                if (!Directory.Exists(outputDir))
                {
                    Directory.CreateDirectory(outputDir);
                }

                cpkArchive.ExtractTo(outputDir);
                Debug.Log($"{cpkFileName} extracted to {outputDir}");
            }
        }

        /// <summary>
        /// Search files using keyword.
        /// </summary>
        /// <param name="keyword"></param>
        /// <returns>File path enumerable</returns>
        public IEnumerable<string> Search(string keyword = "")
        {
            var results = new ConcurrentBag<IEnumerable<string>>();

            Parallel.ForEach(_cpkArchives, archive =>
            {
                var rootNodes = archive.Value.GetRootEntries();
                results.Add(from result in SearchInternal(rootNodes, keyword)
                    select archive.Key + CpkConstants.DirectorySeparator + result);
            });

            var resultList = new List<string>();
            foreach (var result in results)
            {
                resultList.AddRange(result);
            }
            return resultList;
        }

        private void ParseFileVirtualPath(string fullVirtualPath, out string cpkFileName, out string relativeVirtualPath)
        {
            if (!fullVirtualPath.Contains(CpkConstants.DirectorySeparator))
            {
                throw new ArgumentException($"File virtual path is invalid: {fullVirtualPath}.");
            }
            cpkFileName = fullVirtualPath[..fullVirtualPath.IndexOf(CpkConstants.DirectorySeparator)].ToLower();
            relativeVirtualPath = fullVirtualPath[(fullVirtualPath.IndexOf(CpkConstants.DirectorySeparator) + 1)..];
        }

        private IEnumerable<string> SearchInternal(IEnumerable<CpkEntry> nodes, string keyword)
        {
            foreach (CpkEntry node in nodes)
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