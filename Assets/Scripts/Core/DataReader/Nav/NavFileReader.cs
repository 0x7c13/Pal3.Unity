// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Core.DataReader.Nav
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
            var header = reader.ReadChars(4);
            var headerStr = new string(header[..^1]);

            if (headerStr != "NAV")
            {
                throw new InvalidDataException("Invalid NAV(.nav) file: header != NAV");
            }

            var version = reader.ReadByte();
            var numberOfLayers = reader.ReadByte();
            var tileOffset = reader.ReadUInt32();
            var faceOffset = reader.ReadUInt32();

            reader.Seek(tileOffset, SeekOrigin.Begin);
            var tileLayers = new NavTileLayer[numberOfLayers];
            for (var i = 0; i < numberOfLayers; i++)
            {
                tileLayers[i] = ReadTileLayer(reader, version);
            }

            reader.Seek(faceOffset, SeekOrigin.Begin);
            var faceLayers = new NavFaceLayer[numberOfLayers];
            for (var i = 0; i < numberOfLayers; i++)
            {
                faceLayers[i] = ReadFaceLayer(reader);
            }

            return new NavFile(tileLayers, faceLayers);
        }

        private static NavTileLayer ReadTileLayer(IBinaryReader reader, byte version)
        {
            var portals = Array.Empty<GameBoxRect>();

            if (version == 2)
            {
                portals = new GameBoxRect[8];
                for (var i = 0; i < 8; i++)
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

            GameBoxVector3 gameBoxMaxWorldPosition = reader.ReadVector3();
            GameBoxVector3 gameBoxMinWorldPosition = reader.ReadVector3();

            var width = reader.ReadInt32();
            var height = reader.ReadInt32();

            var navMapSize = width * height;
            if (navMapSize <= 0)
            {
                throw new Exception($"Invalid NAV(.nav) file: Map size is in valid: {navMapSize}");
            }

            var tiles = new NavTile[navMapSize];
            for (var i = 0; i < navMapSize; i++)
            {
                tiles[i] = new NavTile
                {
                    GameBoxYPosition = reader.ReadSingle(),
                    DistanceToNearestObstacle = reader.ReadByte(), //---| 1
                    FloorType = (FloorType) reader.ReadByte()  //----| 2
                };
                _ = reader.ReadBytes(2); //-----------------------------| ^ to complete 4-byte alignment
            }

            return new NavTileLayer()
            {
                Portals = portals,
                GameBoxMaxWorldPosition = gameBoxMaxWorldPosition,
                GameBoxMinWorldPosition = gameBoxMinWorldPosition,
                Width = width,
                Height = height,
                Tiles = tiles
            };
        }

        private static NavFaceLayer ReadFaceLayer(IBinaryReader reader)
        {
            var numberOfVertices = reader.ReadUInt16();
            var numberOfFaces = reader.ReadUInt16();
            var gameBoxVertices = new GameBoxVector3[numberOfVertices];
            for (var i = 0; i < numberOfVertices; i++)
            {
                gameBoxVertices[i] = reader.ReadVector3();
            }

            var gameBoxTriangles = new int[numberOfFaces * 3];
            for (var i = 0; i < numberOfFaces; i++)
            {
                var index = i * 3;
                gameBoxTriangles[index]     = reader.ReadUInt16();
                gameBoxTriangles[index + 1] = reader.ReadUInt16();
                gameBoxTriangles[index + 2] = reader.ReadUInt16();
            }

            // Some of the nav meshes in the original game are upside down (don't know why),
            // so we need to flip them if that's the case. You might argue that we can just
            // make the mesh double sided, but that would cause some other issues (trust me).
            {
                GameBoxVector3[] normals = CoreUtility.CalculateNormals(gameBoxVertices, gameBoxTriangles);

                for (var i = 0; i < numberOfFaces; i++)
                {
                    var index = i * 3;

                    // Determine if the face is pointing downwards.
                    if (normals[gameBoxTriangles[index]].Y +
                        normals[gameBoxTriangles[index + 1]].Y +
                        normals[gameBoxTriangles[index + 2]].Y < 0)
                    {
                        // Change the winding order of the face to make it point upwards.
                        (gameBoxTriangles[index], gameBoxTriangles[index + 1]) = (gameBoxTriangles[index + 1], gameBoxTriangles[index]);
                    }
                }
            }

            return new NavFaceLayer()
            {
                GameBoxVertices = gameBoxVertices,
                GameBoxTriangles = gameBoxTriangles,
            };
        }
    }
}