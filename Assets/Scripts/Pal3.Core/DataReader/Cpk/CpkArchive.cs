// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Core.DataReader.Cpk
{
    using System;
    using System.Buffers;
    using System.Collections.Generic;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Text;
    using Lzo;
    using Utilities;

    /// <summary>
    /// Load and create a structural mapping of the internal
    /// file system model within the CPK archive for accessing
    /// the file entities stored inside the archive.
    /// </summary>
    public sealed class CpkArchive
    {
        private const uint CPK_VERSION = 1;
        private const uint CPK_HEADER_MAGIC = 0x_1A_54_53_52;  // CPK header magic label
        private const int CPK_DEFAULT_MAX_NUM_OF_FILE = 32768; // Max number of files per archive

        private readonly string _filePath;
        private readonly Crc32Hash _crcHash;
        private readonly int _codepage;
        private Dictionary<uint, CpkTableEntity> _tableEntities;

        private readonly Dictionary<uint, string> _crcToFileNameMap = new ();
        private readonly Dictionary<uint, uint> _crcToTableIndexMap = new ();
        private readonly Dictionary<uint, HashSet<uint>> _fatherCrcToChildCrcTableIndexMap = new ();

        private bool _archiveInMemory;
        private byte[] _archiveData;

        public CpkArchive(string cpkFilePath, Crc32Hash crcHash, int codepage)
        {
            _filePath = cpkFilePath;
            _crcHash = crcHash;
            _codepage = codepage;
        }

        /// <summary>
        /// Load the CPK file from file system and read header + index table.
        /// </summary>
        public void Init()
        {
            using var stream = new FileStream(_filePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite,
                bufferSize: 8196,
                FileOptions.SequentialScan); // Use SequentialScan option to increase reading speed

            CpkHeader header = CoreUtility.ReadStruct<CpkHeader>(stream);

            if (!IsValidCpkHeader(header))
            {
                throw new InvalidDataException($"File: <{_filePath}> is not a valid CPK file");
            }

            var cpkTableEntitySize = Marshal.SizeOf(typeof(CpkTableEntity));
            var indexTableSize = cpkTableEntitySize * CPK_DEFAULT_MAX_NUM_OF_FILE;
            Span<byte> indexTableBuffer = new byte[indexTableSize];
            // Read the whole index table into memory before processing
            // to avoid I/O overhead
            _ = stream.Read(indexTableBuffer);

            _tableEntities = new Dictionary<uint, CpkTableEntity>((int)header.FileNum);

            var numOfFiles = header.FileNum;
            var maxFileCount = header.MaxFileNum;
            var filesFound = 0;
            var indexTableOffset = 0;
            for (uint i = 0; i < maxFileCount; i++)
            {
                var tableEntity = CoreUtility.ReadStruct<CpkTableEntity>(indexTableBuffer, indexTableOffset);
                indexTableOffset += cpkTableEntitySize;
                if (tableEntity.IsEmpty() || tableEntity.IsDeleted()) continue;

                _tableEntities[i] = tableEntity;
                filesFound++;
                if (numOfFiles == filesFound) break;
            }

            BuildCrcIndexMap();
        }

        /// <summary>
        /// Extract the complete archive to the specified destination
        /// </summary>
        public void ExtractTo(string outputFolder)
        {
            ExtractToInternal(outputFolder, GetRootEntries());
        }

        private void ExtractToInternal(string outputFolder, IEnumerable<CpkEntry> nodes)
        {
            foreach (CpkEntry node in nodes)
            {
                var relativePath = node.VirtualPath.Replace(
                    CpkConstants.DirectorySeparatorChar, Path.DirectorySeparatorChar);

                if (node.IsDirectory)
                {
                    new DirectoryInfo(outputFolder + relativePath).Create();
                    ExtractToInternal(outputFolder, node.Children);
                }
                else
                {
                    uint crcHash = _crcHash.Compute(node.VirtualPath.ToLower(), _codepage);
                    File.WriteAllBytes(outputFolder + relativePath, ReadAllBytes(crcHash));
                }
            }
        }

        /// <summary>
        /// Check if file exists inside the archive.
        /// </summary>
        /// <param name="fileVirtualPath">Virtualized file path inside CPK archive</param>
        /// <param name="filePathCrcHash">CRC hash of the file path</param>
        /// <returns>True if file exists</returns>
        public bool FileExists(string fileVirtualPath, out uint filePathCrcHash)
        {
            filePathCrcHash = _crcHash.Compute(fileVirtualPath.ToLower(), _codepage);
            return _crcToTableIndexMap.ContainsKey(filePathCrcHash);
        }

        /// <summary>
        /// Read all bytes of the give file stored inside the archive.
        /// </summary>
        /// <param name="fileVirtualPathCrcHash">CRC32 hash of the virtualized file path inside CPK archive</param>
        /// <returns>File content in byte array</returns>
        public byte[] ReadAllBytes(uint fileVirtualPathCrcHash)
        {
            CpkTableEntity entity = GetTableEntity(fileVirtualPathCrcHash);

            if (entity.IsDirectory())
            {
                throw new InvalidOperationException(
                    $"Cannot read file <{fileVirtualPathCrcHash}> since it is a directory");
            }

            if (_archiveInMemory)
            {
                ReadOnlySpan<byte> rawData = new ReadOnlySpan<byte>(_archiveData)
                    .Slice((int)entity.StartPos, (int)entity.PackedSize);
                return entity.IsCompressed() ?
                    DecompressDataInArchive(rawData, entity.OriginSize) :
                    rawData.ToArray();
            }
            else
            {
                using var stream = new FileStream(_filePath, FileMode.Open, FileAccess.Read);
                stream.Seek(entity.StartPos, SeekOrigin.Begin);
                var buffer = new byte[entity.PackedSize];
                _ = stream.Read(buffer, 0, (int)entity.PackedSize);
                return entity.IsCompressed() ?
                    DecompressDataInArchive(buffer, entity.OriginSize) :
                    buffer;
            }
        }

        private byte[] DecompressDataInArchive(ReadOnlySpan<byte> compressedData, uint originSize)
        {
            var decompressedData = new byte[originSize];
            MiniLzo.Decompress(compressedData, decompressedData);
            return decompressedData;
        }

        private CpkTableEntity GetTableEntity(uint fileVirtualPathCrcHash)
        {
            if (!_crcToTableIndexMap.ContainsKey(fileVirtualPathCrcHash))
            {
                throw new ArgumentException($"File <{fileVirtualPathCrcHash}> does not exists in the archive");
            }

            return _tableEntities[_crcToTableIndexMap[fileVirtualPathCrcHash]];
        }

        /// <summary>
        /// Preload archive into memory for faster read performance.
        /// </summary>
        public void LoadArchiveIntoMemory()
        {
            _archiveData = File.ReadAllBytes(_filePath);
            _archiveInMemory = true;
        }

        /// <summary>
        /// Dispose in-memory archive data.
        /// </summary>
        public void DisposeInMemoryArchive()
        {
            _archiveInMemory = false;
            _archiveData = null;
        }

        private static bool IsValidCpkHeader(CpkHeader header)
        {
            if (header.Label != CPK_HEADER_MAGIC) return false;
            if (header.Version != CPK_VERSION) return false;
            if (header.TableStart == 0) return false;
            if (header.FileNum > header.MaxFileNum) return false;
            if (header.ValidTableNum > header.MaxTableNum) return false;
            if (header.FileNum > header.ValidTableNum) return false;

            return true;
        }

        private void BuildCrcIndexMap()
        {
            foreach ((var index, CpkTableEntity entity) in _tableEntities)
            {
                _crcToTableIndexMap[entity.CRC] = index;

                if (_fatherCrcToChildCrcTableIndexMap.TryGetValue(entity.FatherCRC, out var childCrcTable))
                {
                    childCrcTable.Add(entity.CRC);
                }
                else
                {
                    _fatherCrcToChildCrcTableIndexMap[entity.FatherCRC] = new HashSet<uint> {entity.CRC};
                }
            }
        }

        /// <summary>
        /// Build a tree structure map of the internal file system
        /// and return the root nodes in CpkEntry format.
        /// </summary>
        /// <returns>Root level CpkEntry nodes</returns>
        public IEnumerable<CpkEntry> GetRootEntries()
        {
            if (_crcToFileNameMap.Count == 0) BuildFileNameMap();
            return GetChildren(0); // 0 is the CRC of the root directory
        }

        private void BuildFileNameMap()
        {
            Stream stream;
            if (_archiveInMemory)
            {
                stream = new MemoryStream(_archiveData);
            }
            else
            {
                stream = new FileStream(_filePath,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.ReadWrite,
                    bufferSize: 8196,
                    FileOptions.RandomAccess);
            }

            foreach (CpkTableEntity entity in _tableEntities.Values)
            {
                byte[] extraInfo = ArrayPool<byte>.Shared.Rent((int)entity.ExtraInfoSize);

                try
                {
                    long extraInfoOffset = entity.StartPos + entity.PackedSize;
                    stream.Seek(extraInfoOffset, SeekOrigin.Begin);
                    _ = stream.Read(extraInfo);
                    string fileName = Encoding.GetEncoding(_codepage)
                        .GetString(extraInfo, 0, Array.IndexOf(extraInfo, (byte)0));
                    _crcToFileNameMap[entity.CRC] = fileName;
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(extraInfo);
                }
            }

            stream.Close();
            stream.Dispose();
        }

        private IEnumerable<CpkEntry> GetChildren(uint fatherCrc, string rootPath = "")
        {
            if (!_fatherCrcToChildCrcTableIndexMap.ContainsKey(fatherCrc))
            {
                yield break;
            }

            if (!string.IsNullOrEmpty(rootPath))
            {
                rootPath += CpkConstants.DirectorySeparatorChar;
            }

            foreach (uint childCrc in _fatherCrcToChildCrcTableIndexMap[fatherCrc])
            {
                uint index = _crcToTableIndexMap[childCrc];
                CpkTableEntity child = _tableEntities[index];
                string fileName = _crcToFileNameMap[child.CRC];

                string virtualPath = rootPath + fileName;

                if (child.IsDirectory())
                {
                    yield return new CpkEntry(virtualPath, true, GetChildren(child.CRC, virtualPath));
                }
                else
                {
                    yield return new CpkEntry(virtualPath, false);
                }
            }
        }
    }
}