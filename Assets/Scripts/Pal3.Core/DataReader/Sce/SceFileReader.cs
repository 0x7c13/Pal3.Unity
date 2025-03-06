// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2025, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Core.DataReader.Sce
{
    using System.IO;

    public sealed class SceFileReader : IFileReader<SceFile>
    {
        public SceFile Read(IBinaryReader reader, int codepage)
        {
            char[] header = reader.ReadChars(4);
            string headerStr = new string(header[..^1]);

            if (headerStr != "SCE")
            {
                throw new InvalidDataException("Invalid SCE(.sce) file: header != SCE");
            }

            byte version = reader.ReadByte();

            if (version != 1)
            {
                throw new InvalidDataException($"Invalid SCE(.sce) file: version ({version}) != 1");
            }

            ushort numberOfBlocks = reader.ReadUInt16();

            SceIndex[] indexes = new SceIndex[numberOfBlocks];
            for (int i = 0; i < numberOfBlocks; i++)
            {
                indexes[i] = ReadSceIndex(reader, codepage);
            }

            SceScriptBlock[] scriptBlocks = new SceScriptBlock[numberOfBlocks];
            for (int i = 0; i < numberOfBlocks; i++)
            {
                scriptBlocks[i] = ReadScriptBlock(reader, indexes[i], codepage);
            }

            return new SceFile(indexes, scriptBlocks, codepage);
        }

        private static SceScriptBlock ReadScriptBlock(IBinaryReader reader, SceIndex sceIndex, int codepage)
        {
            reader.Seek(sceIndex.Offset, SeekOrigin.Begin);

            uint id = reader.ReadUInt32();
            ushort descriptionLength = reader.ReadUInt16();
            string description = reader.ReadString(descriptionLength, codepage);
            ushort numberOfUserVars = reader.ReadUInt16();

            SceUserVarInfo[] userVarInfos = new SceUserVarInfo[numberOfUserVars];
            for (int i = 0; i < numberOfUserVars; i++)
            {
                userVarInfos[i] = ReadUserVarInfo(reader);
            }

            uint scriptSize = reader.ReadUInt32();
            byte[] scriptData = reader.ReadBytes((int)scriptSize);

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
            byte type = reader.ReadByte();
            ushort length = reader.ReadUInt16();
            byte[] data = reader.ReadBytes(length);

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