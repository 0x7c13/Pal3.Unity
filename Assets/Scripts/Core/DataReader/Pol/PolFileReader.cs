// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Core.DataReader.Pol
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using Extensions;
    using GameBox;
    using UnityEngine;
    using Utils;

    public static class PolFileReader
    {
        public static PolFile Read(Stream stream)
        {
            using var reader = new BinaryReader(stream);

            var header = reader.ReadChars(4);
            var headerStr = new string(header);

            if (headerStr != "POLY")
            {
                throw new InvalidDataException($"Invalid POLY(.pol) file: header != POLY");
            }

            var version = reader.ReadInt32();
            var numberOfNodes = reader.ReadInt32();

            var nodeInfos = new List<PolGeometryNode>();
            for (var i = 0; i < numberOfNodes; i++)
            {
                nodeInfos.Add(new PolGeometryNode
                {
                    Name     = reader.ReadGbkString(32),
                    Position = reader.ReadVector3(),
                    Radius   = reader.ReadSingle(),
                    Offset   = reader.ReadInt32()
                });
            }

            if (version > 100)
            {
                var nTag = reader.ReadInt32();
                //Debug.Log($"nTag == {nTag}");
            }

            List<PolMesh> meshInfos = new List<PolMesh>();
            for (var i = 0; i < numberOfNodes; i++)
            {
                meshInfos.Add(ReadMeshData(reader, version));
            }

            return new PolFile(version, nodeInfos.ToArray(), meshInfos.ToArray());
        }

        private static PolMesh ReadMeshData(BinaryReader reader, int version)
        {
            var boundBox = new GameBoxAABBox()
            {
                Min = reader.ReadVector3(),
                Max = reader.ReadVector3(),
            };
            var vertexTypeFlag = reader.ReadUInt32();
            var numberOfVertices = reader.ReadInt32();

            if (numberOfVertices <= 0)
            {
                throw new InvalidDataException($"Invalid POLY(.pol) file: vertices == 0");
            }

            var vertexInfos = new List<PolVertex>();

            for (var i = 0; i < numberOfVertices; i++)
            {
                var vertexInfo = new PolVertex
                {
                    Uv = new Vector2[4]
                };

                if ((vertexTypeFlag & GameBoxVertexType.XYZ) != 0)
                {
                    vertexInfo.Position = reader.ReadVector3();
                }
                if ((vertexTypeFlag & GameBoxVertexType.XYZRHW) != 0)
                {
                    vertexInfo.Position = reader.ReadVector3();
                    _ = reader.ReadSingle();
                }
                if ((vertexTypeFlag & GameBoxVertexType.Normal) != 0)
                {
                    vertexInfo.Normal = reader.ReadVector3();
                }
                if ((vertexTypeFlag & GameBoxVertexType.Diffuse) != 0)
                {
                    vertexInfo.DiffuseColor = Utility.ToColor32(reader.ReadBytes(4));
                }
                if ((vertexTypeFlag & GameBoxVertexType.Specular) != 0)
                {
                    vertexInfo.SpecularColor = Utility.ToColor32(reader.ReadBytes(4));
                }
                if ((vertexTypeFlag & GameBoxVertexType.UV0) != 0)
                {
                    var x = reader.ReadSingle();
                    var y = reader.ReadSingle();
                    vertexInfo.Uv[0] = new Vector2(x, y);
                }
                if ((vertexTypeFlag & GameBoxVertexType.UV1) != 0)
                {
                    var x = reader.ReadSingle();
                    var y = reader.ReadSingle();
                    vertexInfo.Uv[1] = new Vector2(x, y);
                }
                if ((vertexTypeFlag & GameBoxVertexType.UV2) != 0)
                {
                    var x = reader.ReadSingle();
                    var y = reader.ReadSingle();
                    vertexInfo.Uv[2] = new Vector2(x, y);
                }
                if ((vertexTypeFlag & GameBoxVertexType.UV3) != 0)
                {
                    var x = reader.ReadSingle();
                    var y = reader.ReadSingle();
                    vertexInfo.Uv[3] = new Vector2(x, y);
                }

                vertexInfos.Add(vertexInfo);
            }

            var numberOfTextures = reader.ReadInt32();

            var textureInfos = new List<PolTexture>();

            for (var i = 0; i < numberOfTextures; i++)
            {
                textureInfos.Add(ReadTextureInfo(reader, version));
            }

            return new PolMesh()
            {
                BoundBox = boundBox,
                VertexFvfFlag = vertexTypeFlag,
                Vertices = vertexInfos.ToArray(),
                Textures = textureInfos.ToArray()
            };
        }

        private static PolTexture ReadTextureInfo(BinaryReader reader, int version)
        {
            var blendFlag = reader.ReadUInt32();

            var material = new GameBoxMaterial()
            {
                Diffuse = Utility.ToColor(reader.ReadSingleArray(4)),
                Ambient = Utility.ToColor(reader.ReadSingleArray(4)),
                Specular = Utility.ToColor(reader.ReadSingleArray(4)),
                Emissive = Utility.ToColor(reader.ReadSingleArray(4)),
                Power = reader.ReadSingle()
            };

            // Hack fix
            if (material.Power < 0) material.Power = 0;
            else if (material.Power > 128) material.Power = 128;

            var numberOfTextures = reader.ReadInt32();
            var textureNames = new List<string>();

            for (var i = 0; i < numberOfTextures; i++)
            {
                var textureName = reader.ReadGbkString(64);
                textureNames.Add(textureName);
            }

            var indexBits = reader.ReadInt32();
            if (indexBits != 16) throw new Exception($"IndexBits is invalid: {indexBits}");

            var vertStart = reader.ReadInt32();
            var vertEnd = reader.ReadInt32();
            var numberOfTriangles = reader.ReadInt32();

            var triangles = new List<(short x, short y, short z)>();
            for (var i = 0; i < numberOfTriangles; i++)
            {
                var x = reader.ReadInt16();
                var y = reader.ReadInt16();
                var z = reader.ReadInt16();
                triangles.Add(new (x, y, z));
            }

            return new PolTexture()
            {
                BlendFlag = blendFlag,
                Material = material,
                TextureNames = textureNames.ToArray(),
                VertStart = vertStart,
                VertEnd = vertEnd,
                Triangles = triangles.ToArray()
            };
        }
    }
}