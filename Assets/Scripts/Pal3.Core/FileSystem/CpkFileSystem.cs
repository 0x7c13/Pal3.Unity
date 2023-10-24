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
        private readonly ConcurrentDictionary<string, CpkArchive> _cpkArchives = new (StringComparer.OrdinalIgnoreCase);
        private readonly Crc32Hash _crcHash;
        private readonly int _codepage;

        public CpkFileSystem(string rootPath, Crc32Hash crcHash, int codepage)
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
            _codepage = codepage;
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
                throw new FileNotFoundException($"CPK file not found: {cpkFilePath}");
            }

            CpkArchive cpkArchive = new CpkArchive(cpkFilePath, _crcHash, codepage);
            cpkArchive.Init();
            _cpkArchives[cpkFileName] = cpkArchive;
        }

        /// <summary>
        /// Check if file exists in the archive or any of the segmented archives using virtual path.
        /// </summary>
        /// <param name="fileVirtualPath">File virtual path {Cpk file name}\{File relative path inside archive}
        /// Example: music.cpk\music\PI01.mp3</param>
        /// <returns>True if file exists</returns>
        public bool FileExists(string fileVirtualPath)
        {
            return FileExistsInternal(fileVirtualPath, out _, out _);
        }

        /// <summary>
        /// Check if file exists in the archive or any of the segmented archives using virtual path.
        /// </summary>
        /// <param name="fileVirtualPath">File virtual path {Cpk file name}\{File relative path inside archive}
        /// Example: music.cpk\music\PI01.mp3</param>
        /// <param name="archiveName">Name of the archive or the segmented archive if exists</param>
        /// <returns>True if file exists</returns>
        public bool FileExists(string fileVirtualPath,
            out string archiveName)
        {
            return FileExistsInternal(fileVirtualPath, out _, out archiveName);
        }

        /// <summary>
        /// Read all bytes of the given file.
        /// </summary>
        /// <param name="fileVirtualPath">File virtual path inside CPK archive</param>
        /// <returns>Decompressed, ready-to-go file content in byte array</returns>
        public byte[] ReadAllBytes(string fileVirtualPath)
        {
            if (FileExistsInternal(fileVirtualPath,
                    out uint filePathCrcHash,
                    out string archiveName))
            {
                return _cpkArchives[archiveName].ReadAllBytes(filePathCrcHash);
            }

            throw new FileNotFoundException(
                $"The file <{fileVirtualPath}> does not exist in any of the CPK archives");
        }

        private bool FileExistsInternal(string fileVirtualPath,
            out uint filePathCrcHash,
            out string archiveName)
        {
            ParseFileVirtualPath(fileVirtualPath, out string cpkFileName, out string relativeVirtualPath);

            if (_cpkArchives.TryGetValue(cpkFileName, out CpkArchive archive))
            {
                archiveName = cpkFileName;
                return archive.FileExists(relativeVirtualPath, out filePathCrcHash);
            }

            // Prepare the segmented archive prefix to match
            string prefixToMatch = cpkFileName[..^CpkConstants.FileExtension.Length] + '_';

            // Check if the file exists in any of the segmented cpk archives.
            // Ex: q02_01.cpk, q02_02.cpk, q02_03.cpk in PAL3A
            foreach ((string subCpkPath, CpkArchive subArchive) in _cpkArchives)
            {
                // Check if the key starts with the specified prefix
                if (subCpkPath.StartsWith(prefixToMatch, StringComparison.OrdinalIgnoreCase))
                {
                    string relativePathInSubCpk = relativeVirtualPath.Replace(cpkFileName, subCpkPath);

                    if (subArchive.FileExists(relativePathInSubCpk, out filePathCrcHash))
                    {
                        archiveName = subCpkPath;
                        return true;
                    }
                }
            }

            archiveName = null;
            filePathCrcHash = 0;
            return false;
        }

        /// <summary>
        /// Preload archive into memory for faster read performance.
        /// </summary>
        public void LoadArchiveIntoMemory(string cpkFileName)
        {
            if (_cpkArchives.TryGetValue(cpkFileName, out CpkArchive archive))
            {
                archive.LoadArchiveIntoMemory();
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
            if (_cpkArchives.TryGetValue(cpkFileName, out CpkArchive archive))
            {
                archive.DisposeInMemoryArchive();
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
                string outputDir = outputFolder + cpkFileName + Path.DirectorySeparatorChar;

                if (!Directory.Exists(outputDir))
                {
                    Directory.CreateDirectory(outputDir);
                }

                ExtractToInternal(cpkArchive, cpkArchive.GetRootEntries(), outputDir);
            }
        }

        private void ExtractToInternal(CpkArchive archive, IEnumerable<CpkEntry> nodes, string outputFolder)
        {
            foreach (CpkEntry node in nodes)
            {
                string relativePath = node.VirtualPath.Replace(
                    CpkConstants.DirectorySeparatorChar, Path.DirectorySeparatorChar);

                if (node.IsDirectory)
                {
                    new DirectoryInfo(outputFolder + relativePath).Create();
                    ExtractToInternal(archive, node.Children, outputFolder);
                }
                else
                {
                    uint crcHash = _crcHash.Compute(node.VirtualPath.ToLower(), _codepage);
                    File.WriteAllBytes(outputFolder + relativePath, archive.ReadAllBytes(crcHash));
                }
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
        /// <param name="cpkFileName">Cpk file name in lower cases</param>
        /// <param name="relativeVirtualPath">File relative path inside archive in lower cases</param>
        private void ParseFileVirtualPath(string fullVirtualPath, out string cpkFileName, out string relativeVirtualPath)
        {
            if (!fullVirtualPath.Contains(CpkConstants.DirectorySeparatorChar))
            {
                throw new ArgumentException($"File virtual path is invalid: {fullVirtualPath}");
            }

            cpkFileName = fullVirtualPath[..fullVirtualPath.IndexOf(CpkConstants.DirectorySeparatorChar)].ToLower();
            relativeVirtualPath = fullVirtualPath[(fullVirtualPath.IndexOf(CpkConstants.DirectorySeparatorChar) + 1)..].ToLower();
        }
    }
}