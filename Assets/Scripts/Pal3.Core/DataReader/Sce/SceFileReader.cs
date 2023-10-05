// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Core.DataReader.Sce
{
    using System.IO;

    public sealed class SceFileReader : IFileReader<SceFile>
    {
        public SceFile Read(IBinaryReader reader, int codepage)
        {
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
                indexes[i] = ReadSceIndex(reader, codepage);
            }

            var scriptBlocks = new SceScriptBlock[numberOfBlocks];
            for (var i = 0; i < numberOfBlocks; i++)
            {
                scriptBlocks[i] = ReadScriptBlock(reader, indexes[i], codepage);
            }

            return new SceFile(indexes, scriptBlocks, codepage);
        }

        private static SceScriptBlock ReadScriptBlock(IBinaryReader reader, SceIndex sceIndex, int codepage)
        {
            reader.Seek(sceIndex.Offset, SeekOrigin.Begin);

            var id = reader.ReadUInt32();
            var descriptionLength = reader.ReadUInt16();
            var description = reader.ReadString(descriptionLength, codepage);
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

        private static SceUserVarInfo ReadUserVarInfo(IBinaryReader reader)
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

        private static SceIndex ReadSceIndex(IBinaryReader reader, int codepage)
        {
            return new SceIndex()
            {
                Id = reader.ReadUInt32(),
                Offset = reader.ReadUInt32(),
                Description = reader.ReadString(64, codepage)
            };
        }
    }
}