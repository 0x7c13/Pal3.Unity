// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Core.DataReader.Cpk
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Text;
    using Lzo;
    using Utils;

    /// <summary>
    /// Load and create a structural mapping of the internal
    /// file system model within the CPK archive for accessing
    /// the file entities stored inside the archive.
    /// </summary>
    public sealed class CpkArchive
    {
        private const uint SUPPORTED_CPK_VERSION = 1;
        private const uint CPK_HEADER_MAGIC = 0x_1A_54_53_52;  // CPK header magic label
        private const int CPK_DEFAULT_MAX_NUM_OF_FILE = 32768; // Max number of files per archive

        private readonly string _filePath;
        private readonly CrcHash _crcHash;
        private readonly int _codepage;
        private Dictionary<uint, CpkTableEntity> _tableEntities;

        private readonly Dictionary<uint, byte[]> _fileNameMap = new ();
        private readonly Dictionary<uint, uint> _crcToTableIndexMap = new ();
        private readonly Dictionary<uint, HashSet<uint>> _fatherCrcToChildCrcTableIndexMap = new ();
        
        private bool _archiveInMemory;
        private byte[] _archiveData;

        public CpkArchive(string cpkFilePath, CrcHash crcHash, int codepage)
        {
            if (!File.Exists(cpkFilePath))
            {
                throw new ArgumentException($"File does not exists: {cpkFilePath}");
            }

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

            var header = Utility.ReadStruct<CpkHeader>(stream);

            if (!IsValidCpkHeader(header))
            {
                throw new InvalidDataException($"File: {_filePath} is not a valid CPK file.");
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
                var tableEntity = Utility.ReadStruct<CpkTableEntity>(indexTableBuffer, indexTableOffset);
                indexTableOffset += cpkTableEntitySize;
                if (tableEntity.IsEmpty() || tableEntity.IsDeleted()) continue;

                _tableEntities[i] = tableEntity;
                filesFound++;
                if (numOfFiles == filesFound) break;
            }

            BuildCrcIndexMap();
        }

        /// <summary>
        /// Check if file exists inside the archive.
        /// </summary>
        /// <param name="fileVirtualPath">Virtualized file path inside CPK archive</param>
        /// <returns>True if file exists</returns>
        public bool FileExists(string fileVirtualPath)
        {
            var crc = _crcHash.ComputeCrc32Hash(fileVirtualPath.ToLower(), _codepage);
            return _crcToTableIndexMap.ContainsKey(crc);
        }

        /// <summary>
        /// Read all bytes of the give file stored inside the archive.
        /// </summary>
        /// <param name="fileVirtualPath">Virtualized file path inside CPK archive</param>
        /// <returns>File content in byte array</returns>
        public byte[] ReadAllBytes(string fileVirtualPath)
        {
            return _archiveInMemory ?
                ReadAllBytesUsingInMemoryCache(fileVirtualPath) :
                GetFileContent(fileVirtualPath);
        }

        private byte[] ReadAllBytesUsingInMemoryCache(string fileVirtualPath)
        {
            CpkTableEntity entity = ValidateAndGetTableEntity(fileVirtualPath);

            byte[] data;

            var start = (int) entity.StartPos;
            var end = (int) (entity.StartPos + entity.PackedSize);

            if (entity.IsCompressed())
            {
                data = new byte[entity.OriginSize];
                MiniLzo.Decompress(_archiveData[start..end], data);
            }
            else
            {
                data = _archiveData[start..end];
            }

            return data;
        }

        private byte[] GetFileContent(string fileVirtualPath)
        {
            CpkTableEntity entity = ValidateAndGetTableEntity(fileVirtualPath);
            return GetFileContentInternal(entity);
        }

        private byte[] GetFileContentInternal(CpkTableEntity entity)
        {
            byte[] rawData;

            if (_archiveInMemory)
            {
                var start = (int) entity.StartPos;
                var end = (int) (entity.StartPos + entity.PackedSize);
                rawData = _archiveData[start..end];
            }
            else
            {
                using var stream = new FileStream(_filePath, FileMode.Open, FileAccess.Read);
                stream.Seek(entity.StartPos, SeekOrigin.Begin);
                var buffer = new byte[entity.PackedSize];
                _ = stream.Read(buffer, 0, (int)entity.PackedSize);
                rawData = buffer;
            }

            if (!entity.IsCompressed()) return rawData;

            var decompressedData = new byte[entity.OriginSize];
            MiniLzo.Decompress(rawData, decompressedData);
            return decompressedData;
        }

        private CpkTableEntity ValidateAndGetTableEntity(string fileVirtualPath)
        {
            var crc = _crcHash.ComputeCrc32Hash(fileVirtualPath.ToLower(), _codepage);
            if (!_crcToTableIndexMap.ContainsKey(crc))
            {
                throw new ArgumentException($"<{fileVirtualPath}> does not exists in the archive.");
            }

            CpkTableEntity entity = _tableEntities[_crcToTableIndexMap[crc]];

            if (entity.IsDirectory())
            {
                throw new InvalidOperationException($"Cannot open <{fileVirtualPath}> since it is a directory.");
            }

            return entity;
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
            if (header.Version != SUPPORTED_CPK_VERSION) return false;
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

                if (_fatherCrcToChildCrcTableIndexMap.ContainsKey(entity.FatherCRC))
                {
                    _fatherCrcToChildCrcTableIndexMap[entity.FatherCRC].Add(entity.CRC);
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
            if (_fileNameMap.Count == 0) BuildFileNameMap();
            return GetChildren(0);
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
                long extraInfoOffset = entity.StartPos + entity.PackedSize;
                var extraInfo = new byte[entity.ExtraInfoSize];
                stream.Seek(extraInfoOffset, SeekOrigin.Begin);
                _ = stream.Read(extraInfo);

                var fileName = Utility.TrimEnd(extraInfo, new byte[] { 0x00, 0x00 });
                _fileNameMap[entity.CRC] = fileName;
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

            if (rootPath != string.Empty)  rootPath += CpkConstants.DirectorySeparator;

            foreach (var childCrc in _fatherCrcToChildCrcTableIndexMap[fatherCrc])
            {
                var index = _crcToTableIndexMap[childCrc];
                CpkTableEntity child = _tableEntities[index];
                var fileName = Encoding.GetEncoding(_codepage).GetString(_fileNameMap[child.CRC]);

                var virtualPath = rootPath + fileName;

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