// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Core.DataReader.Scn
{
    using System;
    using System.IO;
    using System.Linq;
    using Extensions;
    using GameBox;
    using UnityEngine;

    public static class ScnFileReader
    {
        public static ScnFile Read(Stream stream, int codepage)
        {
            using var reader = new BinaryReader(stream);

            var header = reader.ReadChars(4);
            var headerStr = new string(header[..^1]);

            if (headerStr != "SCN")
            {
                throw new InvalidDataException("Invalid SCN(.scn) file: header != SCN");
            }

            var version = reader.ReadInt16();
            var numberOfNpc = reader.ReadInt16();
            var npcDataOffset = reader.ReadUInt32();
            var numberOfObjects = reader.ReadInt16();
            var objectDataOffset = reader.ReadUInt32();

            var sceneInfo = new ScnSceneInfo()
            {
                CityName = reader.ReadString(32, codepage),
                Name = reader.ReadString(32, codepage),
                Model = reader.ReadString(32, codepage),
                SceneType = (ScnSceneType) reader.ReadInt32(),
                LightMap = reader.ReadInt32(),
                SkyBox = reader.ReadUInt32(),
                Reserved = reader.ReadUInt32Array(6),
            };

            reader.BaseStream.Seek(npcDataOffset, SeekOrigin.Begin);
            var npcInfos = new ScnNpcInfo[numberOfNpc];
            for (var i = 0; i < numberOfNpc; i++)
            {
                npcInfos[i] = ReadNpcInfo(reader, codepage);
            }

            reader.BaseStream.Seek(objectDataOffset, SeekOrigin.Begin);
            var objInfos = new ScnObjectInfo[numberOfObjects];
            for (var i = 0; i < numberOfObjects; i++)
            {
                ScnObjectInfo sceneObject = ReadObjectInfo(reader, codepage);
                objInfos[i] = sceneObject;
            }

            return new ScnFile(sceneInfo, npcInfos, objInfos);
        }

        private static ScnNpcInfo ReadNpcInfo(BinaryReader reader, int codepage)
        {
            return new ScnNpcInfo()
            {
                Id = reader.ReadByte(),
                Kind = (ScnActorKind)reader.ReadByte(),
                Name = reader.ReadString(32, codepage),
                Texture = reader.ReadString(34, codepage),
                FacingDirection = reader.ReadSingle(),
                OnLayer = reader.ReadInt32(),
                GameBoxXPosition = reader.ReadSingle(),
                GameBoxZPosition = reader.ReadSingle(),
                InitActive = reader.ReadInt32(),
                InitBehaviour = (ScnActorBehaviour) reader.ReadInt32(),
                ScriptId = reader.ReadUInt32(),
                GameBoxYPosition = reader.ReadSingle(),
                InitAction = reader.ReadString(16, codepage),
                MonsterId = reader.ReadUInt32Array(3),
                MonsterNumber = reader.ReadUInt16(),
                MonsterRepeat = reader.ReadUInt16(),
                Path = new ScnPath()
                {
                    NumberOfWaypoints = reader.ReadInt32(),
                    GameBoxWaypoints = reader.ReadVector3Array(16)
                },
                NoTurn = reader.ReadUInt32(),
                LoopAction = reader.ReadUInt32(),
                Speed = reader.ReadUInt32(),
                Reserved = reader.ReadUInt32Array(29),
            };
        }

        private static ScnObjectInfo ReadObjectInfo(BinaryReader reader, int codepage)
        {
            #if PAL3
            return new ScnObjectInfo()
            {
                Id = reader.ReadByte(),
                Active = reader.ReadByte(),
                Times = reader.ReadByte(),
                Switch = reader.ReadByte(),

                Name = reader.ReadString(32, codepage),
                
                TriggerType = reader.ReadByte(), // -------------| 1
                NonBlocking = reader.ReadByte(), // -------------| 2
                PaddingBytes = reader.ReadBytes(2), // ----------| ^ to complete 4-byte alignment
                
                GameBoxPosition = reader.ReadVector3(),
                YRotation = reader.ReadSingle(),
                TileMapTriggerRect = new GameBoxRect()
                {
                    Left = reader.ReadInt32(),
                    Top = reader.ReadInt32(),
                    Right = reader.ReadInt32(),
                    Bottom = reader.ReadInt32(),
                },

                Type = (ScnSceneObjectType) reader.ReadByte(),
                SaveState = reader.ReadByte(),
                OnLayer = reader.ReadByte(),
                WuLing = reader.ReadByte(),

                Parameters = reader.ReadInt32Array(6),

                NeedSpecialAction = reader.ReadUInt16(),
                NeedItem = reader.ReadUInt16(),
                NeedGold = reader.ReadUInt16(),
                NeedLv = reader.ReadUInt16(),
                NeedWu = reader.ReadUInt16(),
                NeedAllOpen = reader.ReadUInt16(),
                FailedMessage = reader.ReadString(16, codepage),

                ScriptId = reader.ReadUInt32(),
                Path = new ScnPath()
                {
                    NumberOfWaypoints = reader.ReadInt32(),
                    GameBoxWaypoints = reader.ReadVector3Array(16)
                },

                LinkedObjectId = reader.ReadUInt16(),
                DependentSceneName = reader.ReadString(32, codepage),
                DependentObjectId = reader.ReadUInt16(),

                BoundBox = new GameBoxAABBox()
                {
                    Min = reader.ReadVector3(),
                    Max = reader.ReadVector3()
                },

                XRotation = reader.ReadSingle(),
                SfxName = reader.ReadString(8, codepage),
                EffectModelType = reader.ReadUInt32(),

                ScriptChangeActive = reader.ReadUInt32(),
                ScriptMoved = reader.ReadUInt32(),

                Reserved = reader.ReadUInt32Array(52)
            };
            #elif PAL3A
            return new ScnObjectInfo()
            {
                Id = reader.ReadByte(),
                Active = reader.ReadByte(),
                Times = reader.ReadByte(),
                Switch = reader.ReadByte(),

                Name = reader.ReadString(32, codepage),
                TriggerType = reader.ReadByte(), // -------------| 1
                NonBlocking = reader.ReadByte(), // -------------| 2
                PaddingBytes = reader.ReadBytes(2), // ----------| ^ to complete 4-byte alignment
                
                GameBoxPosition = reader.ReadVector3(),
                YRotation = reader.ReadSingle(),
                TileMapTriggerRect = new GameBoxRect()
                {
                    Left = reader.ReadInt32(),
                    Top = reader.ReadInt32(),
                    Right = reader.ReadInt32(),
                    Bottom = reader.ReadInt32(),
                },

                Type = (ScnSceneObjectType) reader.ReadByte(),
                SaveState = reader.ReadByte(),
                OnLayer = reader.ReadByte(),
                WuLing = reader.ReadByte(),
                
                Parameters = Array.ConvertAll(reader.ReadSingleArray(6), Convert.ToInt32),

                Unknown1 = reader.ReadUInt32(), // TODO

                NeedSpecialAction = reader.ReadUInt16(),
                NeedItem = reader.ReadUInt16(),
                NeedGold = reader.ReadUInt16(),
                NeedLv = reader.ReadUInt16(),
                NeedWu = reader.ReadUInt16(),
                NeedAllOpen = reader.ReadUInt16(),
                FailedMessage = reader.ReadString(16, codepage),

                Unknown2 = reader.ReadUInt32(), // TODO
                
                ScriptId = reader.ReadUInt32(),

                Path = new ScnPath()
                {
                    NumberOfWaypoints = reader.ReadInt32(),
                    GameBoxWaypoints = reader.ReadVector3Array(16)
                },

                LinkedObjectId = reader.ReadUInt16(),
                DependentSceneName = reader.ReadString(32, codepage),
                DependentObjectId = reader.ReadUInt16(),

                BoundBox = new GameBoxAABBox()
                {
                    Min = reader.ReadVector3(),
                    Max = reader.ReadVector3()
                },

                XRotation = reader.ReadSingle(),
                ZRotation = reader.ReadSingle(),
                SfxName = reader.ReadString(8, codepage),
                EffectModelType = reader.ReadUInt32(),

                ScriptChangeActive = reader.ReadUInt32(),
                ScriptMoved = reader.ReadUInt32(),

                Unknown3 = reader.ReadUInt32(), // TODO
                
                Reserved = reader.ReadUInt32Array(44)
            };
            #endif
        }
    }
}