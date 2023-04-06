// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Core.DataReader.Gdb
{
    using System.Collections.Generic;
    using System.IO;
    using Extensions;

    public sealed class GdbFileReader : IFileReader<GdbFile>
    {
        private readonly int _codepage;

        public GdbFileReader(int codepage)
        {
            _codepage = codepage;
        }

        public GdbFile Read(byte[] data)
        {
            using var stream = new MemoryStream(data);
            using var reader = new BinaryReader(stream);

            _ = reader.ReadString(128, _codepage); // header string

            uint actorDataOffset = reader.ReadUInt32();
            int numOfActors = reader.ReadInt32();

            uint skillDataOffset = reader.ReadUInt32();
            int numOfSkills = reader.ReadInt32();

            uint itemDataOffset = reader.ReadUInt32();
            int numOfItems = reader.ReadInt32();

            uint comboSkillDataOffset = reader.ReadUInt32();
            int numOfComboSkills = reader.ReadInt32();

            reader.BaseStream.Seek(itemDataOffset, SeekOrigin.Begin);
            var gameItems = new Dictionary<int, GameItem>();
            for (var i = 0; i < numOfItems; i++)
            {
                GameItem item = ReadGameItem(reader, _codepage);
                gameItems[(int) item.Id] = item;
            }

            return new GdbFile(gameItems);
        }

        private static GameItem ReadGameItem(BinaryReader reader, int codepage)
        {
            return new GameItem
            {
                Id = reader.ReadUInt32(),
                Name = reader.ReadString(32, codepage),
                ModelName = reader.ReadString(30, codepage),
                IconName = reader.ReadString(30, codepage),
                Description = reader.ReadString(512, codepage),
                Price = reader.ReadInt32(),
                Type = (ItemType) reader.ReadByte(),
                WeaponType = (WeaponType) reader.ReadByte(),
                MainActorCanUse = reader.ReadBytes(5),
                WuLing = reader.ReadBytes(5),
                AncientValue = reader.ReadInt32(),
                SpecialType = (SpecialType) reader.ReadByte(), // ----------------| 1
                TargetRangeType = (TargetRangeType) reader.ReadByte(), // --------| 2
                PlaceOfUseType = (PlaceOfUseType) reader.ReadByte(), // ----------| 3
                AttributeImpactType = reader.ReadBytes(12),
                PaddingByte = reader.ReadByte(), // ------------------------------| ^ to complete 4-byte alignment
                AttributeImpactValue = reader.ReadInt16Array(12),
                FightStateImpactType = reader.ReadBytes(32),
                FightStateImpactValue = reader.ReadInt16Array(32),
                ComboCount = reader.ReadInt32(),
                QiSavingPercentage = reader.ReadInt16(),
                MpSavingPercentage = reader.ReadInt16(),
                CriticalAttackAmplifyPercentage = reader.ReadInt16(),
                SpecialSkillSuccessRate = reader.ReadInt16(),
                OreId = reader.ReadUInt32(),
                ProductId = reader.ReadUInt32(),
                ProductPrice = reader.ReadInt32(),
                SynthesisMaterialIds = reader.ReadUInt32Array(2),
                SynthesisProductId = reader.ReadUInt32()
            };
        }
    }
}