// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE.txt in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Core.DataReader.Nav
{
    using System;
    using System.Collections.Generic;
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
            var tileLayers = new List<NavTileLayer>();
            for (var i = 0; i < numberOfLayers; i++)
            {
                tileLayers.Add(ReadTileLayer(reader, version));
            }

            reader.BaseStream.Seek(faceOffset, SeekOrigin.Begin);
            var faceLayers = new List<NavFaceLayer>();
            for (var i = 0; i < numberOfLayers; i++)
            {
                faceLayers.Add(ReadFaceLayer(reader, version));
            }

            return new NavFile(tileLayers.ToArray(), faceLayers.ToArray());
        }

        private static NavTileLayer ReadTileLayer(BinaryReader reader, byte version)
        {
            var portals = new List<GameBoxRect>();
            if (version == 2)
            {
                for (var i = 0; i < 8; i++)
                {
                    portals.Add(new GameBoxRect()
                    {
                        Left = reader.ReadInt32(),
                        Top = reader.ReadInt32(),
                        Right = reader.ReadInt32(),
                        Bottom = reader.ReadInt32(),
                    });
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

            var tiles = new List<NavTile>();
            for (var i = 0; i < navMapSize; i++)
            {
                tiles.Add(new NavTile()
                {
                    Y = reader.ReadSingle(),
                    Distance = reader.ReadUInt16(),
                    FloorKind = (NavFloorKind) reader.ReadUInt16()
                });
            }

            return new NavTileLayer()
            {
                Portals = portals.ToArray(),
                Max = max,
                Min = min,
                Width = width,
                Height = height,
                Tiles = tiles.ToArray()
            };
        }

        private static NavFaceLayer ReadFaceLayer(BinaryReader reader, byte version)
        {
            var numberOfVertices = reader.ReadUInt16();
            var numberOfFaces = reader.ReadUInt16();
            var vertices = new List<Vector3>();
            for (var i = 0; i < numberOfVertices; i++)
            {
                vertices.Add(reader.ReadVector3());
            }

            var triangles = new List<int>();
            for (var i = 0; i < numberOfFaces; i++)
            {
                triangles.Add(reader.ReadUInt16());
                triangles.Add(reader.ReadUInt16());
                triangles.Add(reader.ReadUInt16());
            }

            return new NavFaceLayer()
            {
                Vertices = vertices.ToArray(),
                Triangles = triangles.ToArray(),
            };
        }
    }
}