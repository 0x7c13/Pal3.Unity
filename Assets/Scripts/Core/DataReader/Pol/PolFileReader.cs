// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Core.DataReader.Pol
{
    using System;
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

            var nodeInfos = new PolGeometryNode[numberOfNodes];
            for (var i = 0; i < numberOfNodes; i++)
            {
                nodeInfos[i] = new PolGeometryNode
                {
                    Name     = reader.ReadGbkString(32),
                    Position = reader.ReadVector3(),
                    Radius   = reader.ReadSingle(),
                    Offset   = reader.ReadInt32()
                };
            }

            if (version > 100)
            {
                var nTag = reader.ReadInt32();
                //Debug.Log($"nTag == {nTag}");
            }

            var meshInfos = new PolMesh[numberOfNodes];
            for (var i = 0; i < numberOfNodes; i++)
            {
                meshInfos[i] = ReadMeshData(reader, version);
            }

            return new PolFile(version, nodeInfos, meshInfos);
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

            var positions = new Vector3[numberOfVertices];
            var normals = new Vector3[numberOfVertices];
            var diffuseColors = new Color32[numberOfVertices];
            var radii = new float[numberOfVertices];
            var specularColors = new Color32[numberOfVertices];
            var uvs = new Vector2[4][];

            uvs[0] = new Vector2[numberOfVertices];
            uvs[1] = new Vector2[numberOfVertices];
            uvs[2] = new Vector2[numberOfVertices];
            uvs[3] = new Vector2[numberOfVertices];

            for (var i = 0; i < numberOfVertices; i++)
            {
                if ((vertexTypeFlag & GameBoxVertexType.XYZ) != 0)
                {
                    positions[i] = GameBoxInterpreter
                        .ToUnityVertex(reader.ReadVector3(),
                            GameBoxInterpreter.GameBoxUnitToUnityUnit);
                }
                if ((vertexTypeFlag & GameBoxVertexType.XYZRHW) != 0)
                {
                    positions[i] = GameBoxInterpreter
                        .ToUnityVertex(reader.ReadVector3(),
                            GameBoxInterpreter.GameBoxUnitToUnityUnit);
                    _ = reader.ReadSingle();
                }
                if ((vertexTypeFlag & GameBoxVertexType.Normal) != 0)
                {
                    normals[i] = GameBoxInterpreter
                        .ToUnityVertex(reader.ReadVector3(),
                            GameBoxInterpreter.GameBoxUnitToUnityUnit);
                }
                if ((vertexTypeFlag & GameBoxVertexType.Diffuse) != 0)
                {
                    diffuseColors[i] = Utility.ToColor32(reader.ReadBytes(4));
                }
                if ((vertexTypeFlag & GameBoxVertexType.Specular) != 0)
                {
                    specularColors[i] = Utility.ToColor32(reader.ReadBytes(4));
                }
                if ((vertexTypeFlag & GameBoxVertexType.UV0) != 0)
                {
                    var x = reader.ReadSingle();
                    var y = reader.ReadSingle();
                    uvs[0][i] = new Vector2(x, y);
                }
                if ((vertexTypeFlag & GameBoxVertexType.UV1) != 0)
                {
                    var x = reader.ReadSingle();
                    var y = reader.ReadSingle();
                    uvs[1][i] = new Vector2(x, y);
                }
                if ((vertexTypeFlag & GameBoxVertexType.UV2) != 0)
                {
                    var x = reader.ReadSingle();
                    var y = reader.ReadSingle();
                    uvs[2][i] = new Vector2(x, y);
                }
                if ((vertexTypeFlag & GameBoxVertexType.UV3) != 0)
                {
                    var x = reader.ReadSingle();
                    var y = reader.ReadSingle();
                    uvs[3][i] = new Vector2(x, y);
                }
            }

            var vertexInfo = new PolVertexInfo()
            {
                Positions = positions,
                Normals = normals,
                DiffuseColors = diffuseColors,
                Radii = radii,
                SpecularColors = specularColors,
                Uvs = uvs,
            };

            var numberOfTextures = reader.ReadInt32();

            var textureInfos = new PolTexture[numberOfTextures];

            for (var i = 0; i < numberOfTextures; i++)
            {
                textureInfos[i] = ReadTextureInfo(reader, version);
            }

            return new PolMesh
            {
                BoundBox = boundBox,
                VertexFvfFlag = vertexTypeFlag,
                VertexInfo = vertexInfo,
                Textures = textureInfos
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
            var textureNames = new string[numberOfTextures];

            for (var i = 0; i < numberOfTextures; i++)
            {
                var textureName = reader.ReadGbkString(64);
                textureNames[i] = textureName;
            }

            var indexBits = reader.ReadInt32();
            if (indexBits != 16) throw new Exception($"IndexBits is invalid: {indexBits}");

            var vertStart = reader.ReadInt32();
            var vertEnd = reader.ReadInt32();
            var numberOfFaces = reader.ReadInt32();

            var triangles = new int[numberOfFaces * 3];
            for (var i = 0; i < numberOfFaces; i++)
            {
                var index = i * 3;
                var x = reader.ReadInt16();
                var y = reader.ReadInt16();
                var z = reader.ReadInt16();
                (triangles[index], triangles[index + 1], triangles[index + 2]) =
                    GameBoxInterpreter.ToUnityTriangle((x, y, z));
            }

            return new PolTexture()
            {
                BlendFlag = blendFlag,
                Material = material,
                TextureNames = textureNames,
                VertStart = vertStart,
                VertEnd = vertEnd,
                Triangles = triangles
            };
        }
    }
}