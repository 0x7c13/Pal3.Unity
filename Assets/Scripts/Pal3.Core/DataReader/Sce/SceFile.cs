// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2024, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Core.DataReader.Sce
{
    using System.Collections.Generic;

    // SCE (.sce) file header
    public struct SceHeader
    {
        public string Magic; // 4 bytes
        public byte Version;
        public ushort NumberOfBlocks;
    }

    public struct SceIndex
    {
        public uint Id;
        public uint Offset;
        public string Description; // 64 chars
    }

    public struct SceUserVarInfo
    {
        public byte Type;
        public byte[] InitData;
    }

    public struct SceScriptBlock
    {
        public uint Id;
        public string Description;
        public byte[] ScriptData;
        public SceUserVarInfo[] UserVariables;
    }

    /// <summary>
    /// SCE (.sce) file model
    /// </summary>
    public sealed class SceFile
    {
        public SceIndex[] Indexes { get; }
        public Dictionary<uint, SceScriptBlock> ScriptBlocks { get; }
        public int Codepage { get; }

        public SceFile(SceIndex[] indexes, SceScriptBlock[] scriptBlocks, int codepage)
        {
            Indexes = indexes;

            Dictionary<uint, SceScriptBlock> scriptBlocksDic = new();
            foreach (SceScriptBlock block in scriptBlocks)
            {
                scriptBlocksDic[block.Id] = block;
            }

            ScriptBlocks = scriptBlocksDic;
            Codepage = codepage;
        }
    }
}