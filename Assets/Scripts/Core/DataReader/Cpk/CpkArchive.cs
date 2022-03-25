// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Core.DataReader.Cpk
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Text;
    using Lzo;
    using Utils;

    /// <summary>
    /// CpkArchive
    /// Load and create a tree structure mapping of the internal
    /// file system model within the CPK archive.
    /// </summary>
    public class CpkArchive
    {
        private const uint SUPPORTED_CPK_VERSION = 1;
        private const uint CPK_HEADER_MAGIC = 0x_1A_54_53_52;  // CPK header magic label
        private const int CPK_DEFAULT_MAX_NUM_OF_FILE = 32768; // Max number of files per archive
        private const int GBK_CODE_PAGE = 936; // GBK Encoding's code page

        private readonly string _filePath;
        private Dictionary<uint, CpkTableEntity> _tableEntities;

        private readonly Dictionary<uint, byte[]> _fileNameMap = new ();
        private readonly Dictionary<uint, uint> _crcToTableIndexMap = new ();
        private readonly Dictionary<uint, HashSet<uint>> _fatherCrcToChildCrcTableIndexMap = new ();

        private readonly CrcHash _crcHash;

        private bool _archiveInMemory;
        private byte[] _archiveData;

        public CpkArchive(string cpkFilePath, CrcHash crcHash)
        {
            if (!File.Exists(cpkFilePath))
            {
                throw new ArgumentException($"File does not exists: {cpkFilePath}");
            }

            _filePath = cpkFilePath;
            _crcHash = crcHash;

            Init();
        }

        /// <summary>
        /// Load the CPK file from file system.
        /// </summary>
        private void Init()
        {
            using var stream = new FileStream(_filePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite,
                bufferSize: 8196,
                FileOptions.SequentialScan); // use SequentialScan option to increase reading speed

            var header = Utility.ReadStruct<CpkHeader>(stream);

            if (!IsValidCpkHeader(header))
            {
                throw new InvalidDataException($"File: {_filePath} is not a valid CPK file.");
            }

            var cpkTableEntitySize = Marshal.SizeOf(typeof(CpkTableEntity));
            var indexTableSize = cpkTableEntitySize * CPK_DEFAULT_MAX_NUM_OF_FILE;
            var indexTableBuffer = new byte[indexTableSize];
            // Read the whole index table into memory before processing
            // to avoid I/O overhead
            stream.Read(indexTableBuffer, 0, indexTableSize);

            _tableEntities = new Dictionary<uint, CpkTableEntity>((int)header.FileNum);

            var numOfFiles = header.FileNum;
            var filesFound = 0;
            var offset = 0;
            for (uint i = 0; i < header.MaxFileNum; i++)
            {
                var tableEntity = Utility.ReadStruct<CpkTableEntity>(indexTableBuffer, offset);
                offset += cpkTableEntitySize;
                if (tableEntity.IsEmpty() || tableEntity.IsDeleted()) continue;

                _tableEntities[i] = tableEntity;
                filesFound++;
                if (numOfFiles == filesFound) break;
            }

            BuildCrcIndexMap();
        }

        /// <summary>
        /// Check if file exists inside the archive using the
        /// virtual path.
        /// </summary>
        /// <param name="fileVirtualPath">Virtualized file path inside CPK archive</param>
        /// <returns>True if file exists</returns>
        public bool FileExists(string fileVirtualPath)
        {
            var crc = _crcHash.ComputeCrc32Hash(fileVirtualPath.ToLower());
            return _crcToTableIndexMap.ContainsKey(crc);
        }

        /// <summary>
        /// Read all bytes of the give file
        /// </summary>
        /// <param name="fileVirtualPath">Virtualized file path inside CPK archive</param>
        /// <returns>File content in byte array</returns>
        public byte[] ReadAllBytes(string fileVirtualPath)
        {
            if (_archiveInMemory)
            {
                return ReadAllBytesUsingInMemoryCache(fileVirtualPath);
            }

            using var stream = Open(fileVirtualPath, out var size, out _);
            var buffer = new byte[size];
            stream.Read(buffer, 0, (int)size);
            return buffer;
        }

        private byte[] ReadAllBytesUsingInMemoryCache(string fileVirtualPath)
        {
            var entity = ValidateAndGetTableEntity(fileVirtualPath);

            byte[] buffer;

            var start = (int) entity.StartPos;
            var end = (int) (entity.StartPos + entity.PackedSize);

            if (entity.IsCompressed())
            {
                buffer = new byte[entity.OriginSize];
                MiniLzo.Decompress(_archiveData[start..end], buffer);
            }
            else
            {
                buffer = _archiveData[start..end];
            }

            return buffer;
        }

        /// <summary>
        /// Open and create a Stream pointing to the CPK's internal file location
        /// of the given virtual file path and populate the size of the file.
        /// </summary>
        /// <param name="fileVirtualPath">Virtualized file path inside CPK archive</param>
        /// <param name="size">Size of the file</param>
        /// <param name="isCompressed">True if file is compressed</param>
        /// <returns>A Stream used to read the content</returns>
        /// <exception cref="ArgumentException">Throw if file does not exists</exception>
        /// <exception cref="InvalidOperationException">Throw if given file path is a directory</exception>
        public Stream Open(string fileVirtualPath, out uint size, out bool isCompressed)
        {
            var entity = ValidateAndGetTableEntity(fileVirtualPath);
            return OpenInternal(entity, out size, out isCompressed);
        }

        private Stream OpenInternal(CpkTableEntity entity, out uint size, out bool isCompressed)
        {
            Stream stream;
            if (_archiveInMemory)
            {
                var start = (int) entity.StartPos;
                var end = (int) (entity.StartPos + entity.PackedSize);
                stream = new MemoryStream(_archiveData[start..end]);
            }
            else
            {
                stream = new FileStream(_filePath, FileMode.Open, FileAccess.Read);
                stream.Seek(entity.StartPos, SeekOrigin.Begin);
            }

            if (entity.IsCompressed())
            {
                size = entity.OriginSize;
                isCompressed = true;

                var src = new byte[entity.PackedSize];
                stream.Read(src, 0, (int)entity.PackedSize);
                stream.Dispose();
                var buffer = new byte[entity.OriginSize];
                MiniLzo.Decompress(src, buffer);
                return new MemoryStream(buffer);
            }
            else
            {
                size = entity.PackedSize;
                isCompressed = false;
                return stream;
            }
        }

        private CpkTableEntity ValidateAndGetTableEntity(string fileVirtualPath)
        {
            var crc = _crcHash.ComputeCrc32Hash(fileVirtualPath.ToLower());
            if (!_crcToTableIndexMap.ContainsKey(crc))
            {
                throw new ArgumentException($"<{fileVirtualPath}> does not exists in the archive.");
            }

            var entity = _tableEntities[_crcToTableIndexMap[crc]];

            if (entity.IsDirectory())
            {
                throw new InvalidOperationException($"Cannot open <{fileVirtualPath}> since it is a directory.");
            }

            return entity;
        }

        /// <summary>
        /// Preload archive into memory for faster read performance
        /// </summary>
        public void LoadArchiveIntoMemory()
        {
            _archiveData = File.ReadAllBytes(_filePath);
            _archiveInMemory = true;
        }

        /// <summary>
        /// Dispose in-memory archive data
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
            foreach (var tableKv in _tableEntities)
            {
                var entity = tableKv.Value;

                _crcToTableIndexMap[entity.CRC] = tableKv.Key;

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
        /// Build a tree structure map of the internal files
        /// and return the root nodes in CpkEntry format
        /// </summary>
        /// <returns>Root level CpkEntry nodes</returns>
        public IList<CpkEntry> GetRootEntries()
        {
            if (_fileNameMap.Count == 0) BuildFileNameMap();
            return GetChildren(0).ToList();
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

            foreach (var entity in _tableEntities.Values)
            {
                long extraInfoOffset = entity.StartPos + entity.PackedSize;
                var extraInfo = new byte[entity.ExtraInfoSize];
                stream.Seek(extraInfoOffset, SeekOrigin.Begin);
                stream.Read(extraInfo);

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

            if (rootPath != string.Empty)  rootPath += CpkConstants.CpkDirectorySeparatorChar;

            foreach (var childCrc in _fatherCrcToChildCrcTableIndexMap[fatherCrc])
            {
                var index = _crcToTableIndexMap[childCrc];
                var child = _tableEntities[index];
                var fileName = Encoding.GetEncoding(GBK_CODE_PAGE).GetString(_fileNameMap[child.CRC]);

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