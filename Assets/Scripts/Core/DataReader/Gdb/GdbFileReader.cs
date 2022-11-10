// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Core.DataReader.Gdb
{
    using System.IO;
    using Extensions;

    public static class GdbFileReader
    {
        public static void Read(byte[] data, int codepage)
        {
            using var stream = new MemoryStream(data);
            using var reader = new BinaryReader(stream);

            var headerStr = reader.ReadString(128, codepage);

            uint actorDataOffset = reader.ReadUInt32();
            int numOfActors = reader.ReadInt32();

            uint skillDataOffset = reader.ReadUInt32();
            int numOfSkills = reader.ReadInt32();
            
            uint itemDataOffset = reader.ReadUInt32();
            int numOfItems = reader.ReadInt32();
            
            uint comboDataOffset = reader.ReadUInt32();
            int numOfCombos = reader.ReadInt32();

            reader.BaseStream.Position = itemDataOffset;
            var gameItems = new GameItem[numOfItems];
            for (int i = 0; i < numOfItems; i++)
            {
                gameItems[i] = ReadGameItem(reader, codepage);
            }
        }
        
        public static GameItem ReadGameItem(BinaryReader reader, int codepage)
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
                SpecialType = (SpecialType) reader.ReadByte(),
                TargetRangeType = (TargetRangeType) reader.ReadByte(),
                PlaceOfUseType = (PlaceOfUseType) reader.ReadByte(),
                AttributeImpactType = reader.ReadBytes(13),
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