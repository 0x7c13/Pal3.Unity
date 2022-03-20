// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Core.DataReader.Sce
{
    using System.IO;
    using Extensions;

    public static class SceFileReader
    {
        public static SceFile Read(Stream stream)
        {
            using var reader = new BinaryReader(stream);

            var header = reader.ReadChars(4);
            var headerStr = new string(header[..^1]);

            if (headerStr != "SCE")
            {
                throw new InvalidDataException("Invalid SCE(.sce) file: header != SCE");
            }

            var version = reader.ReadByte();

            if (version != 1)
            {
                throw new InvalidDataException($"Invalid SCE(.sce) file: version ({version}) != 1");
            }

            var numberOfBlocks = reader.ReadUInt16();

            var indexes = new SceIndex[numberOfBlocks];
            for (var i = 0; i < numberOfBlocks; i++)
            {
                indexes[i] = ReadSceIndex(reader);
            }

            var scriptBlocks = new SceScriptBlock[numberOfBlocks];
            for (var i = 0; i < numberOfBlocks; i++)
            {
                scriptBlocks[i] = ReadScriptBlock(reader, indexes[i]);
            }

            return new SceFile(indexes, scriptBlocks);
        }

        private static SceScriptBlock ReadScriptBlock(BinaryReader reader, SceIndex sceIndex)
        {
            reader.BaseStream.Seek(sceIndex.Offset, SeekOrigin.Begin);

            var id = reader.ReadUInt32();
            var descriptionLength = reader.ReadUInt16();
            var description = reader.ReadGbkString(descriptionLength);
            var numberOfUserVars = reader.ReadUInt16();

            var userVarInfos = new SceUserVarInfo[numberOfUserVars];
            for (var i = 0; i < numberOfUserVars; i++)
            {
                userVarInfos[i] = ReadUserVarInfo(reader);
            }

            var scriptSize = reader.ReadUInt32();
            var scriptData = reader.ReadBytes((int)scriptSize);

            return new SceScriptBlock()
            {
                Id = id,
                Description = description,
                ScriptData = scriptData,
                UserVariables = userVarInfos
            };
        }

        private static SceUserVarInfo ReadUserVarInfo(BinaryReader reader)
        {
            var type = reader.ReadByte();
            var length = reader.ReadUInt16();
            var data = reader.ReadBytes(length);

            return new SceUserVarInfo()
            {
                Type = type,
                InitData = data
            };
        }

        private static SceIndex ReadSceIndex(BinaryReader reader)
        {
            return new SceIndex()
            {
                Id = reader.ReadUInt32(),
                Offset = reader.ReadUInt32(),
                Description = reader.ReadGbkString(64)
            };
        }
    }
}