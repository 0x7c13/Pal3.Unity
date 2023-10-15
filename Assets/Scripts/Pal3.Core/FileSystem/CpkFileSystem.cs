// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Core.FileSystem
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using DataReader.Cpk;
    using Utilities;

    /// <summary>
    /// File system wrapper for CPack archives
    /// </summary>
    public sealed class CpkFileSystem : ICpkFileSystem
    {
        private readonly string _rootPath;
        private readonly ConcurrentDictionary<string, CpkArchive> _cpkArchives = new();
        private readonly Crc32Hash _crcHash;

        public CpkFileSystem(string rootPath, Crc32Hash crcHash)
        {
            if (!rootPath.EndsWith(Path.DirectorySeparatorChar))
            {
                rootPath += Path.DirectorySeparatorChar;
            }

            if (!Directory.Exists(rootPath))
            {
                throw new DirectoryNotFoundException(rootPath);
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
            string cpkFileName = CoreUtility.GetFileName(cpkFileRelativePath, Path.DirectorySeparatorChar).ToLower();

            if (_cpkArchives.ContainsKey(cpkFileName))
            {
                return;
            }

            string cpkFilePath = _rootPath + cpkFileRelativePath;

            if (!File.Exists(cpkFilePath))
            {
                throw new FileNotFoundException(cpkFilePath);
            }

            var cpkArchive = new CpkArchive(cpkFilePath, _crcHash, codepage);
            cpkArchive.Init();
            _cpkArchives[cpkFileName] = cpkArchive;
        }

        /// <summary>
        /// Check if file exists in any of the segmented archives using virtual path.
        /// </summary>
        /// <param name="fileVirtualPath">File virtual path {Cpk file name}\{File relative path inside archive}</param>
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
                         .Where(_ => _.StartsWith(cpkFileName[..^CpkConstants.FileExtension.Length] + '_',
                             StringComparison.OrdinalIgnoreCase)))
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
        /// <param name="fileVirtualPath">File virtual path {Cpk file name}\{File relative path inside archive}
        /// Example: music.cpk\music\PI01.mp3</param>
        /// <returns>True if file exists</returns>
        public bool FileExists(string fileVirtualPath)
        {
            ParseFileVirtualPath(fileVirtualPath, out var cpkFileName, out var relativeVirtualPath);

            if (_cpkArchives.TryGetValue(cpkFileName, out CpkArchive archive))
            {
                return archive.FileExists(relativeVirtualPath);
            }

            // Check if the file exists in any of the segmented cpk archives.
            foreach (var subCpkPath in _cpkArchives.Keys
                         .Where(_ => _.StartsWith(cpkFileName[..^CpkConstants.FileExtension.Length] + '_',
                             StringComparison.OrdinalIgnoreCase)))
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

            if (_cpkArchives.TryGetValue(cpkFileName, out CpkArchive archive))
            {
                return archive.ReadAllBytes(relativeVirtualPath);
            }

            // Find and read the file content in segmented cpk archives.
            foreach (var subCpkPath in _cpkArchives.Keys
                         .Where(_ => _.StartsWith(cpkFileName[..^CpkConstants.FileExtension.Length] + '_',
                             StringComparison.OrdinalIgnoreCase)))
            {
                var relativePathInSubCpk = relativeVirtualPath.Replace(cpkFileName, subCpkPath);
                if (_cpkArchives[subCpkPath].FileExists(relativePathInSubCpk))
                {
                    return _cpkArchives[subCpkPath].ReadAllBytes(relativePathInSubCpk);
                }
            }

            throw new FileNotFoundException($"<{fileVirtualPath}> not found");
        }

        /// <summary>
        /// Preload archive into memory for faster read performance.
        /// </summary>
        public void LoadArchiveIntoMemory(string cpkFileName)
        {
            if (_cpkArchives.ContainsKey(cpkFileName.ToLower()))
            {
                _cpkArchives[cpkFileName.ToLower()].LoadArchiveIntoMemory();
            }
            else
            {
                throw new Exception($"<{cpkFileName}> archive not mounted yet");
            }
        }

        /// <summary>
        /// Dispose in-memory archive data.
        /// </summary>
        public void DisposeInMemoryArchive(string cpkFileName)
        {
            if (_cpkArchives.ContainsKey(cpkFileName.ToLower()))
            {
                _cpkArchives[cpkFileName.ToLower()].DisposeInMemoryArchive();
            }
            else
            {
                throw new Exception($"<{cpkFileName}> archive not mounted yet");
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
            }
        }

        /// <summary>
        /// Searches for files in the file system that match the specified keyword.
        /// </summary>
        /// <param name="keyword">The keyword to search for. If empty, all files will be returned.</param>
        /// <returns>A list of file paths that match the specified keyword.</returns>
        public IList<string> Search(string keyword = "")
        {
            var results = new ConcurrentBag<IEnumerable<string>>();

            Parallel.ForEach(_cpkArchives, archive =>
            {
                var rootNodes = archive.Value.GetRootEntries();
                results.Add(from result in SearchInternal(rootNodes, keyword)
                    select archive.Key + CpkConstants.DirectorySeparatorChar + result);
            });

            var resultList = new List<string>();
            foreach (var result in results)
            {
                resultList.AddRange(result);
            }
            return resultList;
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
                else if (node.VirtualPath.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    yield return node.VirtualPath;
                }
            }
        }

        /// <summary>
        /// Searches for files in the CPK file system that match the specified keywords.
        /// </summary>
        /// <param name="keywords">The list of keywords to search for.</param>
        /// <returns>A dictionary where the keys are the file paths of the matching files,
        /// and the values are the lines in the files that contain the keywords.</returns>
        public IDictionary<string, IList<string>> BatchSearch(IList<string> keywords)
        {
            var results = new ConcurrentDictionary<string, ConcurrentBag<string>>();
            foreach (string keyword in keywords)
            {
                results[keyword] = new ConcurrentBag<string>();
            }

            Parallel.ForEach(_cpkArchives, archive =>
            {
                var rootNodes = archive.Value.GetRootEntries();

                foreach (var match in SearchInternal(rootNodes, keywords))
                {
                    foreach (var keyword in keywords)
                    {
                        if (match.Value.Contains(keyword))
                        {
                            var resultPath = archive.Key + CpkConstants.DirectorySeparatorChar + match.Key;
                            results[keyword].Add(resultPath);
                        }
                    }
                }
            });

            return results.ToDictionary<KeyValuePair<string, ConcurrentBag<string>>,
                string, IList<string>>(result => result.Key,
                result => result.Value.ToList());
        }

        private IDictionary<string, HashSet<string>> SearchInternal(IEnumerable<CpkEntry> nodes, IList<string> keywords)
        {
            var results = new Dictionary<string, HashSet<string>>();

            foreach (CpkEntry node in nodes)
            {
                if (node.IsDirectory)
                {
                    foreach (var result in SearchInternal(node.Children, keywords))
                    {
                        if (!results.ContainsKey(result.Key))
                        {
                            results[result.Key] = new HashSet<string>();
                        }
                        foreach (var path in result.Value)
                        {
                            results[result.Key].Add(path);
                        }
                    }
                }
                else
                {
                    foreach (var keyword in keywords)
                    {
                        if (!string.IsNullOrEmpty(node.VirtualPath) &&
                            node.VirtualPath.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            if (!results.ContainsKey(node.VirtualPath))
                            {
                                results[node.VirtualPath] = new HashSet<string>();
                            }
                            results[node.VirtualPath].Add(keyword);
                        }
                    }
                }
            }

            return results;
        }

        /// <summary>
        /// Parse file virtual path into cpk file name and relative virtual path.
        /// </summary>
        /// <param name="fullVirtualPath">File virtual path {Cpk file name}\{File relative path inside archive}</param>
        /// <param name="cpkFileName">{Cpk file name}</param>
        /// <param name="relativeVirtualPath">{File relative path inside archive}</param>
        private void ParseFileVirtualPath(string fullVirtualPath, out string cpkFileName, out string relativeVirtualPath)
        {
            if (!fullVirtualPath.Contains(CpkConstants.DirectorySeparatorChar))
            {
                throw new ArgumentException($"File virtual path is invalid: {fullVirtualPath}");
            }

            cpkFileName = fullVirtualPath[..fullVirtualPath.IndexOf(CpkConstants.DirectorySeparatorChar)].ToLower();
            relativeVirtualPath = fullVirtualPath[(fullVirtualPath.IndexOf(CpkConstants.DirectorySeparatorChar) + 1)..];
        }
    }
}