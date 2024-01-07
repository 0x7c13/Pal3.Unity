// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2024, Jiaqi (0x7c13) Liu. All rights reserved.
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

        private bool _isInitialized;
        private IDictionary<uint, CpkTableEntity> _crcToTableEntityMap;
        private IDictionary<uint, HashSet<uint>> _fatherCrcToChildCrcTableIndexMap;

        // Lazy init since it's not needed for core functionality
        private Lazy<IDictionary<uint, string>> _crcToFileNameMap;

        private bool _isArchiveInMemory;
        private byte[] _inMemoryArchiveData;

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
            if (_isInitialized) return;

            using Stream stream = _isArchiveInMemory ?
                new MemoryStream(_inMemoryArchiveData) :
                new FileStream(_filePath,
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

            int cpkTableEntitySize = Marshal.SizeOf(typeof(CpkTableEntity));
            int indexTableSize = cpkTableEntitySize * CPK_DEFAULT_MAX_NUM_OF_FILE;
            byte[] indexTableBuffer = ArrayPool<byte>.Shared.Rent(indexTableSize);

            try
            {
                // Read the whole index table into memory before processing
                // to avoid I/O overhead
                _ = stream.Read(indexTableBuffer, 0, indexTableSize);

                uint numOfFiles = header.NumberOfFiles;
                int filesFound = 0;
                int indexTableOffset = 0;

                _crcToTableEntityMap = new Dictionary<uint, CpkTableEntity>((int)numOfFiles);
                _fatherCrcToChildCrcTableIndexMap = new Dictionary<uint, HashSet<uint>>((int)numOfFiles);

                for (uint i = 0; i < header.MaxFileCount; i++)
                {
                    CpkTableEntity tableEntity = CoreUtility.ReadStruct<CpkTableEntity>(
                        indexTableBuffer.AsSpan(), indexTableOffset);
                    indexTableOffset += cpkTableEntitySize;

                    // Skip empty and deleted entries
                    if (tableEntity.IsEmpty() || tableEntity.IsDeleted()) continue;

                    _crcToTableEntityMap[tableEntity.CRC] = tableEntity;

                    if (_fatherCrcToChildCrcTableIndexMap.TryGetValue(
                            tableEntity.FatherCRC, out HashSet<uint> childCrcTable))
                    {
                        childCrcTable.Add(tableEntity.CRC);
                    }
                    else
                    {
                        _fatherCrcToChildCrcTableIndexMap[tableEntity.FatherCRC] =
                            new HashSet<uint> {tableEntity.CRC};
                    }

                    if (++filesFound == numOfFiles) break; // break early if all files are found
                }

                // File names are not needed for core functionality, so we lazy init it.
                // It will only be used for Search/Extract operations.
                _crcToFileNameMap = new Lazy<IDictionary<uint, string>>(GenerateFileNameMap);
                _isInitialized = true;
            }
            finally
            {
                 ArrayPool<byte>.Shared.Return(indexTableBuffer);
            }
        }

        private IDictionary<uint, string> GenerateFileNameMap()
        {
            Dictionary<uint, string> crcToFileNameMap = new (_crcToTableEntityMap.Values.Count);

            using Stream stream = _isArchiveInMemory ?
                new MemoryStream(_inMemoryArchiveData) :
                new FileStream(_filePath,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.ReadWrite,
                    bufferSize: 8196,
                    FileOptions.RandomAccess);

            foreach ((uint crc, CpkTableEntity entity) in _crcToTableEntityMap)
            {
                int extraInfoSize = (int)entity.ExtraInfoSize;
                byte[] extraInfoBuffer = ArrayPool<byte>.Shared.Rent(extraInfoSize);

                try
                {
                    long extraInfoOffset = entity.StartPos + entity.PackedSize;
                    stream.Seek(extraInfoOffset, SeekOrigin.Begin);
                    _ = stream.Read(extraInfoBuffer, 0, extraInfoSize);
                    string fileName = Encoding.GetEncoding(_codepage)
                        .GetString(extraInfoBuffer, 0, Array.IndexOf(extraInfoBuffer, (byte)0));
                    crcToFileNameMap[crc] = fileName;
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(extraInfoBuffer);
                }
            }

            return crcToFileNameMap;
        }

        /// <summary>
        /// Check if file exists inside the archive.
        /// </summary>
        /// <param name="fileVirtualPath">Virtualized file path inside CPK archive</param>
        /// <param name="filePathCrcHash">CRC hash of the file path</param>
        /// <returns>True if file exists</returns>
        public bool FileExists(string fileVirtualPath, out uint filePathCrcHash)
        {
            if (!_isInitialized)
            {
                throw new InvalidOperationException("Initialize the archive before accessing its content");
            }
            filePathCrcHash = _crcHash.Compute(fileVirtualPath.ToLower(), _codepage);
            return _crcToTableEntityMap.ContainsKey(filePathCrcHash);
        }

        /// <summary>
        /// Read all bytes of the give file stored inside the archive.
        /// </summary>
        /// <param name="fileVirtualPathCrcHash">CRC32 hash of the virtualized file path inside CPK archive</param>
        /// <returns>File content in byte array</returns>
        public byte[] ReadAllBytes(uint fileVirtualPathCrcHash)
        {
            if (!_isInitialized)
            {
                throw new InvalidOperationException("Initialize the archive before accessing its content");
            }

            if (!_crcToTableEntityMap.ContainsKey(fileVirtualPathCrcHash))
            {
                throw new FileNotFoundException(
                    $"File <{fileVirtualPathCrcHash}> does not exists in the archive");
            }

            CpkTableEntity entity = _crcToTableEntityMap[fileVirtualPathCrcHash];

            if (entity.IsDirectory())
            {
                throw new InvalidOperationException(
                    $"Cannot read file <{fileVirtualPathCrcHash}> since it is a directory");
            }

            if (_isArchiveInMemory)
            {
                Span<byte> rawData = _inMemoryArchiveData.AsSpan()
                    .Slice((int)entity.StartPos, length: (int)entity.PackedSize);
                return entity.IsCompressed() ?
                    DecompressDataInArchive(rawData, entity.OriginSize) :
                    rawData.ToArray();
            }
            else
            {
                using var stream = new FileStream(_filePath, FileMode.Open, FileAccess.Read);
                stream.Seek(entity.StartPos, SeekOrigin.Begin);
                byte[] buffer = new byte[entity.PackedSize];
                _ = stream.Read(buffer.AsSpan());
                return entity.IsCompressed() ?
                    DecompressDataInArchive(buffer.AsSpan(), entity.OriginSize) :
                    buffer;
            }
        }

        private byte[] DecompressDataInArchive(Span<byte> compressedData, uint originSize)
        {
            byte[] decompressedData = new byte[originSize];
            MiniLzo.Decompress(compressedData, decompressedData);
            return decompressedData;
        }

        /// <summary>
        /// Preload archive into memory for faster read performance.
        /// </summary>
        public void LoadArchiveIntoMemory()
        {
            if (_isArchiveInMemory) return;
            _inMemoryArchiveData = File.ReadAllBytes(_filePath);
            _isArchiveInMemory = true;
        }

        /// <summary>
        /// Dispose in-memory archive data.
        /// </summary>
        public void DisposeInMemoryArchive()
        {
            if (!_isArchiveInMemory) return;
            _isArchiveInMemory = false;
            _inMemoryArchiveData = null;
        }

        /// <summary>
        /// Build a tree structure map of the internal file system
        /// and return the root nodes in CpkEntry format.
        /// </summary>
        /// <returns>Root level CpkEntry nodes</returns>
        public IEnumerable<CpkEntry> GetRootEntries()
        {
            if (!_isInitialized)
            {
                throw new InvalidOperationException("Initialize the archive before accessing its content");
            }

            return GetChildren(0); // 0 is the CRC of the root directory
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
                CpkTableEntity child = _crcToTableEntityMap[childCrc];
                string fileName = _crcToFileNameMap.Value[child.CRC];

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

        private static bool IsValidCpkHeader(CpkHeader header)
        {
            if (header.Label != CPK_HEADER_MAGIC) return false;
            if (header.Version != CPK_VERSION) return false;
            if (header.TableStart == 0) return false;
            if (header.NumberOfFiles > header.MaxFileCount) return false;
            if (header.ValidTableNum > header.MaxTableNum) return false;
            if (header.NumberOfFiles > header.ValidTableNum) return false;

            return true;
        }
    }
}