// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Core.DataReader.Scn
{
    using System.IO;
    using Extensions;
    using GameBox;
    using UnityEngine;

    public static class ScnFileReader
    {
        public static ScnFile Read(Stream stream)
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
                CityName = reader.ReadGbkString(32),
                Name = reader.ReadGbkString(32),
                Model = reader.ReadGbkString(32),
                SceneType = (ScnSceneType) reader.ReadInt32(),
                LightMap = reader.ReadInt32(),
                SkyBox = reader.ReadUInt32(),
                Reserved = reader.ReadUInt32Array(6),
            };

            reader.BaseStream.Seek(npcDataOffset, SeekOrigin.Begin);
            var npcInfos = new ScnNpcInfo[numberOfNpc];
            for (var i = 0; i < numberOfNpc; i++)
            {
                var npcInfo = ReadNpcInfo(reader);
                if (npcInfo.Unknown1[0] != 0 ||
                    npcInfo.Unknown1[1] != 0 ||
                    npcInfo.Unknown2[0] != 0 ||
                    npcInfo.Unknown2[1] != 0 ||
                    npcInfo.Unknown2[2] != 0)
                {
                    Debug.LogError($"Unknown byte has value for ActorId: {npcInfo.Id} Name: {npcInfo.Name}");
                }
                npcInfos[i] = npcInfo;
            }

            reader.BaseStream.Seek(objectDataOffset, SeekOrigin.Begin);
            var objInfos = new ScnObjectInfo[numberOfObjects];
            for (var i = 0; i < numberOfObjects; i++)
            {
                var sceneObject = ReadObjectInfo(reader);
                objInfos[i] = sceneObject;
            }

            return new ScnFile(sceneInfo, npcInfos, objInfos);
        }

        private static ScnNpcInfo ReadNpcInfo(BinaryReader reader)
        {
            return new ScnNpcInfo()
            {
                Id = reader.ReadByte(),
                Kind = (ScnActorKind)reader.ReadByte(),
                Name = reader.ReadGbkString(32),
                Texture = reader.ReadGbkString(32),
                Unknown1 = reader.ReadBytes(2),
                FacingDirection = reader.ReadSingle(),
                OnLayer = reader.ReadByte(),
                Unknown2 = reader.ReadBytes(3),
                PositionX = reader.ReadSingle(),
                PositionZ = reader.ReadSingle(),
                InitActive = reader.ReadInt32(),
                InitBehaviour = (ScnActorBehaviour) reader.ReadInt32(),
                ScriptId = reader.ReadUInt32(),
                PositionY = reader.ReadSingle(),
                InitAction = reader.ReadGbkString(16),
                MonsterId = reader.ReadUInt32Array(3),
                MonsterNumber = reader.ReadUInt16(),
                MonsterRepeat = reader.ReadUInt16(),
                Path = new ScnPath()
                {
                    NumberOfWaypoints = reader.ReadInt32(),
                    Waypoints = reader.ReadVector3Array(16)
                },
                NoTurn = reader.ReadUInt32(),
                LoopAction = reader.ReadUInt32(),
                Speed = reader.ReadUInt32(),
                Reserved = reader.ReadUInt32Array(29),
            };
        }

        private static ScnObjectInfo ReadObjectInfo(BinaryReader reader)
        {
            #if PAL3
            return new ScnObjectInfo()
            {
                Id = reader.ReadByte(),
                Active = reader.ReadByte(),
                Times = reader.ReadByte(),
                Switch = reader.ReadByte(),

                Name = reader.ReadGbkString(32),
                TriggerType = reader.ReadUInt16(),
                NonBlocking = reader.ReadUInt16(),

                Position = reader.ReadVector3(),
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
                FailedMessage = reader.ReadGbkString(16),

                ScriptId = reader.ReadUInt32(),
                Path = new ScnPath()
                {
                    NumberOfWaypoints = reader.ReadInt32(),
                    Waypoints = reader.ReadVector3Array(16)
                },

                LinkedObject = reader.ReadUInt16(),
                DependentSceneName = reader.ReadGbkString(32),
                DependentId = reader.ReadUInt16(),

                BoundBox = new GameBoxAABBox()
                {
                    Min = reader.ReadVector3(),
                    Max = reader.ReadVector3()
                },

                XRotation = reader.ReadSingle(),
                SfxName = reader.ReadGbkString(8),
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

                Name = reader.ReadGbkString(32),
                TriggerType = reader.ReadUInt16(),
                NonBlocking = reader.ReadUInt16(),

                Position = reader.ReadVector3(),
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

                // TODO:
                Unknown1 = reader.ReadBytes(4),

                Parameters = reader.ReadInt32Array(6),

                NeedSpecialAction = reader.ReadUInt16(),
                NeedItem = reader.ReadUInt16(),
                NeedGold = reader.ReadUInt16(),
                NeedLv = reader.ReadUInt16(),
                NeedWu = reader.ReadUInt16(),
                NeedAllOpen = reader.ReadUInt16(),
                FailedMessage = reader.ReadGbkString(16),

                // TODO:
                Unknown2 = reader.ReadBytes(4),
                ScriptId = reader.ReadUInt32(),

                Path = new ScnPath()
                {
                    NumberOfWaypoints = reader.ReadInt32(),
                    Waypoints = reader.ReadVector3Array(16)
                },

                LinkedObject = reader.ReadUInt16(),
                DependentSceneName = reader.ReadGbkString(32),
                DependentId = reader.ReadUInt16(),

                BoundBox = new GameBoxAABBox()
                {
                    Min = reader.ReadVector3(),
                    Max = reader.ReadVector3()
                },

                XRotation = reader.ReadSingle(),
                SfxName = reader.ReadGbkString(8),
                EffectModelType = reader.ReadUInt32(),

                ScriptChangeActive = reader.ReadUInt32(),
                ScriptMoved = reader.ReadUInt32(),

                Reserved = reader.ReadUInt32Array(46)
            };
            #endif
        }
    }
}