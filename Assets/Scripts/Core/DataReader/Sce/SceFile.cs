// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE.txt in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Core.DataReader.Sce
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
    public class SceFile
    {
        public SceIndex[] Indexes { get; }
        public Dictionary<uint, SceScriptBlock> ScriptBlocks { get; }

        public SceFile(SceIndex[] indexes, SceScriptBlock[] scriptBlocks)
        {
            Indexes = indexes;

            var scriptBlocksDic = new Dictionary<uint, SceScriptBlock>();
            foreach (var block in scriptBlocks)
            {
                scriptBlocksDic[block.Id] = block;
            }

            ScriptBlocks = scriptBlocksDic;
        }
    }
}