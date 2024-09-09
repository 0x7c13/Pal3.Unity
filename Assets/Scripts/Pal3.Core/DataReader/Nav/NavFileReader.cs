﻿// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2024, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Core.DataReader.Nav
{
    using System;
    using System.IO;
    using Contract.Enums;
    using Primitives;
    using Utilities;

    public sealed class NavFileReader : IFileReader<NavFile>
    {
        public NavFile Read(IBinaryReader reader, int codepage)
        {
            char[] header = reader.ReadChars(4);
            string headerStr = new string(header[..^1]);

            if (headerStr != "NAV")
            {
                throw new InvalidDataException("Invalid NAV(.nav) file: header != NAV");
            }

            byte version = reader.ReadByte();
            byte numberOfLayers = reader.ReadByte();
            uint tileOffset = reader.ReadUInt32();
            uint faceOffset = reader.ReadUInt32();

            reader.Seek(tileOffset, SeekOrigin.Begin);
            NavLayer[] layers = new NavLayer[numberOfLayers];
            for (int i = 0; i < numberOfLayers; i++)
            {
                layers[i] = ReadLayerData(reader, version);
            }

            reader.Seek(faceOffset, SeekOrigin.Begin);
            NavMeshData[] meshData = new NavMeshData[numberOfLayers];
            for (int i = 0; i < numberOfLayers; i++)
            {
                meshData[i] = ReadMeshData(reader);
            }

            return new NavFile(layers, meshData);
        }

        private static NavLayer ReadLayerData(IBinaryReader reader, byte version)
        {
            GameBoxRect[] portals = Array.Empty<GameBoxRect>();

            if (version == 2)
            {
                portals = new GameBoxRect[8];
                for (int i = 0; i < 8; i++)
                {
                    portals[i] = new GameBoxRect()
                    {
                        Left = reader.ReadInt32(),
                        Top = reader.ReadInt32(),
                        Right = reader.ReadInt32(),
                        Bottom = reader.ReadInt32(),
                    };
                }
            }

            GameBoxVector3 gameBoxMaxWorldPosition = reader.ReadGameBoxVector3();
            GameBoxVector3 gameBoxMinWorldPosition = reader.ReadGameBoxVector3();

            int width = reader.ReadInt32();
            int height = reader.ReadInt32();

            int navMapSize = width * height;
            if (navMapSize <= 0)
            {
                throw new Exception($"Invalid NAV(.nav) file: Map size is in valid: {navMapSize}");
            }

            NavTile[] tiles = new NavTile[navMapSize];
            for (int i = 0; i < navMapSize; i++)
            {
                tiles[i] = new NavTile
                {
                    GameBoxYPosition = reader.ReadSingle(),
                    DistanceToNearestObstacle = reader.ReadByte(), //---| 1
                    FloorType = (FloorType) reader.ReadByte()     //----| 2
                };
                _ = reader.ReadBytes(2); //-----------------------------| ^ to complete 4-byte alignment
            }

            return new NavLayer()
            {
                Portals = portals,
                GameBoxMaxWorldPosition = gameBoxMaxWorldPosition,
                GameBoxMinWorldPosition = gameBoxMinWorldPosition,
                Width = width,
                Height = height,
                Tiles = tiles
            };
        }

        private static NavMeshData ReadMeshData(IBinaryReader reader)
        {
            ushort numberOfVertices = reader.ReadUInt16();
            ushort numberOfFaces = reader.ReadUInt16();

            GameBoxVector3[] gameBoxVertices = reader.ReadGameBoxVector3s(numberOfVertices);

            int[] gameBoxTriangles = new int[numberOfFaces * 3];
            for (int i = 0; i < numberOfFaces; i++)
            {
                int index = i * 3;
                gameBoxTriangles[index]     = reader.ReadUInt16();
                gameBoxTriangles[index + 1] = reader.ReadUInt16();
                gameBoxTriangles[index + 2] = reader.ReadUInt16();
            }

            // Some of the nav meshes in the original game are upside down (don't know why),
            // so we need to flip them if that's the case. You might argue that we can just
            // make the mesh double sided, but that would cause some other issues (trust me).
            {
                GameBoxVector3[] normals = CoreUtility.CalculateNormals(gameBoxVertices, gameBoxTriangles);

                for (int i = 0; i < numberOfFaces; i++)
                {
                    int index = i * 3;

                    // Determine if the face is pointing downwards.
                    if (normals[gameBoxTriangles[index]].Y +
                        normals[gameBoxTriangles[index + 1]].Y +
                        normals[gameBoxTriangles[index + 2]].Y < 0)
                    {
                        // Change the winding order of the face to make it point upwards.
                        (gameBoxTriangles[index], gameBoxTriangles[index + 1]) =
                            (gameBoxTriangles[index + 1], gameBoxTriangles[index]);
                    }
                }
            }

            return new NavMeshData()
            {
                GameBoxVertices = gameBoxVertices,
                GameBoxTriangles = gameBoxTriangles,
            };
        }
    }
}