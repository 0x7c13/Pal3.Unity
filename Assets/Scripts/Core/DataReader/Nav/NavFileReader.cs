// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Core.DataReader.Nav
{
    using System;
    using System.IO;
    using Extensions;
    using GameBox;
    using UnityEngine;

    public static class NavFileReader
    {
        public static NavFile Read(Stream stream)
        {
            using var reader = new BinaryReader(stream);

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

            reader.BaseStream.Seek(tileOffset, SeekOrigin.Begin);
            var tileLayers = new NavTileLayer[numberOfLayers];
            for (var i = 0; i < numberOfLayers; i++)
            {
                tileLayers[i] = ReadTileLayer(reader, version);
            }

            reader.BaseStream.Seek(faceOffset, SeekOrigin.Begin);
            var faceLayers = new NavFaceLayer[numberOfLayers];
            for (var i = 0; i < numberOfLayers; i++)
            {
                faceLayers[i] = ReadFaceLayer(reader, version);
            }

            return new NavFile(tileLayers, faceLayers);
        }

        private static NavTileLayer ReadTileLayer(BinaryReader reader, byte version)
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

            var max = reader.ReadVector3();
            var min = reader.ReadVector3();

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
                    Y = reader.ReadSingle(),
                    Distance = reader.ReadUInt16(),
                    FloorKind = (NavFloorKind) reader.ReadUInt16()
                };
            }

            return new NavTileLayer()
            {
                Portals = portals,
                Max = max,
                Min = min,
                Width = width,
                Height = height,
                Tiles = tiles
            };
        }

        private static NavFaceLayer ReadFaceLayer(BinaryReader reader, byte version)
        {
            var numberOfVertices = reader.ReadUInt16();
            var numberOfFaces = reader.ReadUInt16();
            var vertices = new Vector3[numberOfVertices];
            for (var i = 0; i < numberOfVertices; i++)
            {
                vertices[i] = reader.ReadVector3();
            }

            var triangles = new int[numberOfFaces * 3];
            for (var i = 0; i < numberOfFaces; i++)
            {
                var index = i * 3;
                triangles[index]     = reader.ReadUInt16();
                triangles[index + 1] = reader.ReadUInt16();
                triangles[index + 2] = reader.ReadUInt16();
            }

            return new NavFaceLayer()
            {
                Vertices = vertices,
                Triangles = triangles,
            };
        }
    }
}