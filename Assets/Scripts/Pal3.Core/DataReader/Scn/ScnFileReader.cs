// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2025, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Core.DataReader.Scn
{
    using System;
    using System.IO;
    using Contract.Enums;
    using Primitives;

    public sealed class ScnFileReader : IFileReader<ScnFile>
    {
        public ScnFile Read(IBinaryReader reader, int codepage)
        {
            char[] header = reader.ReadChars(4);
            string headerStr = new (header[..^1]);

            if (headerStr != "SCN")
            {
                throw new InvalidDataException("Invalid SCN(.scn) file: header != SCN");
            }

            short version = reader.ReadInt16();
            short numberOfNpc = reader.ReadInt16();
            uint npcDataOffset = reader.ReadUInt32();
            short numberOfObjects = reader.ReadInt16();
            uint objectDataOffset = reader.ReadUInt32();

            ScnSceneInfo sceneInfo = new()
            {
                CityName = reader.ReadString(32, codepage),
                SceneName = reader.ReadString(32, codepage),
                Model = reader.ReadString(32, codepage),
                SceneType = (SceneType) reader.ReadInt32(),
                LightMap = reader.ReadInt32(),
                SkyBox = reader.ReadUInt32(),
                Reserved = reader.ReadUInt32s(6),
            };

            reader.Seek(npcDataOffset, SeekOrigin.Begin);
            ScnNpcInfo[] npcInfos = new ScnNpcInfo[numberOfNpc];
            for (int i = 0; i < numberOfNpc; i++)
            {
                npcInfos[i] = ReadNpcInfo(reader, codepage);
            }

            reader.Seek(objectDataOffset, SeekOrigin.Begin);
            ScnObjectInfo[] objInfos = new ScnObjectInfo[numberOfObjects];
            for (int i = 0; i < numberOfObjects; i++)
            {
                ScnObjectInfo sceneObject = ReadObjectInfo(reader, codepage);
                objInfos[i] = sceneObject;
            }

            return new ScnFile(sceneInfo, npcInfos, objInfos);
        }

        private static ScnNpcInfo ReadNpcInfo(IBinaryReader reader, int codepage)
        {
            return new ScnNpcInfo()
            {
                Id = reader.ReadByte(),
                Type = (ActorType)reader.ReadByte(),
                Name = reader.ReadString(32, codepage),
                Texture = reader.ReadString(34, codepage),
                FacingDirection = reader.ReadSingle(),
                LayerIndex = reader.ReadInt32(),
                GameBoxXPosition = reader.ReadSingle(),
                GameBoxZPosition = reader.ReadSingle(),
                InitActive = reader.ReadInt32(),
                InitBehaviour = (ActorBehaviourType) reader.ReadInt32(),
                ScriptId = reader.ReadUInt32(),
                GameBoxYPosition = reader.ReadSingle(),
                InitAction = reader.ReadString(16, codepage),
                MonsterIds = reader.ReadUInt32s(3),
                NumberOfMonsters = reader.ReadByte(),
                MonsterCanRespawn = reader.ReadByte(),
                PaddingBytes = reader.ReadBytes(2), // Empty padding bytes to complete 4-byte alignment
                Path = new ScnPath()
                {
                    NumberOfWaypoints = reader.ReadInt32(),
                    GameBoxWaypoints = reader.ReadGameBoxVector3s(16)
                },
                NoTurn = reader.ReadUInt32(),
                LoopAction = reader.ReadUInt32(),
                GameBoxMoveSpeed = reader.ReadUInt32(),
                Reserved = reader.ReadUInt32s(29),
            };
        }

        private static ScnObjectInfo ReadObjectInfo(IBinaryReader reader, int codepage)
        {
            byte id = reader.ReadByte();
            byte initActive = reader.ReadByte();
            byte times = reader.ReadByte();
            byte switchState = reader.ReadByte();

            string name = reader.ReadString(32, codepage);

            byte triggerType = reader.ReadByte();
            byte isNonBlocking = reader.ReadByte();
            _ = reader.ReadBytes(2); // Empty padding bytes to complete 4-byte alignment

            GameBoxVector3 gameBoxPosition = reader.ReadGameBoxVector3();
            float yRotation = reader.ReadSingle();

            GameBoxRect tileMapTriggerRect = new()
            {
                Left = reader.ReadInt32(),
                Top = reader.ReadInt32(),
                Right = reader.ReadInt32(),
                Bottom = reader.ReadInt32(),
            };

            SceneObjectType type = (SceneObjectType)reader.ReadByte();
            byte saveState = reader.ReadByte();
            byte layerIndex = reader.ReadByte();
            ObjectElementType elementType = (ObjectElementType)reader.ReadByte();

            #if PAL3
            int[] parameters = reader.ReadInt32s(6);
            #elif PAL3A
            int[] parameters = Array.ConvertAll(reader.ReadSingles(6), Convert.ToInt32);
            #endif

            #if PAL3A
            uint notUsed = reader.ReadUInt32();
            #endif

            byte requireSpecialAction = reader.ReadByte();
            ushort requireItem = reader.ReadUInt16();
            _ = reader.ReadByte(); // Empty padding byte to complete 4-byte alignment

            ushort requireMoney = reader.ReadUInt16();
            ushort requireLevel = reader.ReadUInt16();

            ushort requireAttackValue = reader.ReadUInt16();
            byte requireAllMechanismsSolved = reader.ReadByte();
            string failedMessage = reader.ReadString(16, codepage);
            _ = reader.ReadByte(); // Empty padding byte to complete 4-byte alignment

            #if PAL3A
            ushort linkedObjectGroupId = reader.ReadUInt16();
            _ = reader.ReadBytes(2); // Empty padding bytes to complete 4-byte alignment
            #endif

            uint scriptId = reader.ReadUInt32();

            ScnPath path = new()
            {
                NumberOfWaypoints = reader.ReadInt32(),
                GameBoxWaypoints = reader.ReadGameBoxVector3s(16)
            };

            ushort linkedObjectId = reader.ReadUInt16();
            string dependentSceneName = reader.ReadString(32, codepage);
            byte dependentObjectId = reader.ReadByte();
            _ = reader.ReadByte(); // Empty padding byte to complete 4-byte alignment

            GameBoxVector3 gameBoxBoundsMin = reader.ReadGameBoxVector3();
            GameBoxVector3 gameBoxBoundsMax = reader.ReadGameBoxVector3();

            float xRotation = reader.ReadSingle();

            #if PAL3A
            float zRotation = reader.ReadSingle();
            #endif

            string sfxName = reader.ReadString(8, codepage);

            uint effectModelType = reader.ReadUInt32();

            uint scriptActivated = reader.ReadUInt32();
            uint scriptMoved = reader.ReadUInt32();

            #if PAL3A
            uint canOnlyBeTriggeredOnce = reader.ReadUInt32();
            #endif

            #if PAL3
            uint[] reserved = reader.ReadUInt32s(52);
            #elif PAL3A
            uint[] reserved = reader.ReadUInt32s(44);
            #endif

            return new ScnObjectInfo()
            {
                Id = id,
                InitActive = initActive,
                Times = times,
                SwitchState = switchState,

                Name = name,

                TriggerType = triggerType,
                IsNonBlocking = isNonBlocking,

                GameBoxPosition = gameBoxPosition,
                GameBoxYRotation = yRotation,
                TileMapTriggerRect = tileMapTriggerRect,

                Type = type,
                SaveState = saveState,
                LayerIndex = layerIndex,
                ElementType = elementType,

                Parameters = parameters,

                #if PAL3A
                NotUsed = notUsed,
                #endif

                RequireSpecialAction = requireSpecialAction,
                RequireItem = requireItem,
                RequireMoney = requireMoney,
                RequireLevel = requireLevel,
                RequireAttackValue = requireAttackValue,
                RequireAllMechanismsSolved = requireAllMechanismsSolved,

                FailedMessage = failedMessage,

                #if PAL3A
                LinkedObjectGroupId = linkedObjectGroupId,
                #endif

                ScriptId = scriptId,

                Path = path,

                LinkedObjectId = linkedObjectId,
                DependentSceneName = dependentSceneName,
                DependentObjectId = dependentObjectId,

                GameBoxBoundsMin = gameBoxBoundsMin,
                GameBoxBoundsMax = gameBoxBoundsMax,

                GameBoxXRotation = xRotation,

                #if PAL3A
                GameBoxZRotation = zRotation,
                #endif

                SfxName = sfxName,
                EffectModelType = effectModelType,

                ScriptActivated = scriptActivated,
                ScriptMoved = scriptMoved,

                #if PAL3A
                CanOnlyBeTriggeredOnce = canOnlyBeTriggeredOnce,
                #endif

                Reserved = reserved,
            };
        }
    }
}