// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

#define USE_UNSAFE_BINARY_READER

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
        public static PolFile Read(byte[] data, int codepage)
        {
            #if USE_UNSAFE_BINARY_READER
            using var reader = new UnsafeBinaryReader(data);
            #else
            using var stream = new MemoryStream(data);
            using var reader = new BinaryReader(stream);
            #endif

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
                    Name     = reader.ReadString(32, codepage),
                    Position = GameBoxInterpreter.ToUnityPosition(reader.ReadVector3()),
                    Radius   = reader.ReadSingle(),
                    Offset   = reader.ReadInt32()
                };
            }

            var tagNodes = Array.Empty<TagNode>();
            if (version > 100)
            {
                var numberOfTagNodes = reader.ReadInt32();
                if (numberOfTagNodes > 0)
                {
                    tagNodes = new TagNode[numberOfTagNodes];
                    for (var i = 0; i < numberOfTagNodes; i++)
                    {
                        tagNodes[i] = ReadTagNodeInfo(reader, codepage);
                    }
                }
            }

            var meshInfos = new PolMesh[numberOfNodes];
            for (var i = 0; i < numberOfNodes; i++)
            {
                meshInfos[i] = ReadMeshData(reader, version, codepage);
            }

            return new PolFile(version, nodeInfos, meshInfos, tagNodes);
        }

        #if USE_UNSAFE_BINARY_READER
        private static TagNode ReadTagNodeInfo(UnsafeBinaryReader reader, int codepage)
        #else
        private static TagNode ReadTagNodeInfo(BinaryReader reader, int codepage)
        #endif
        {
            var name = reader.ReadString(32, codepage);
            Matrix4x4 transformMatrix = GameBoxInterpreter.ToUnityMatrix4x4(new GameBoxMatrix4X4()
            {
                Xx = reader.ReadSingle(), Xy = reader.ReadSingle(), Xz = reader.ReadSingle(), Xw = reader.ReadSingle(),
                Yx = reader.ReadSingle(), Yy = reader.ReadSingle(), Yz = reader.ReadSingle(), Yw = reader.ReadSingle(),
                Zx = reader.ReadSingle(), Zy = reader.ReadSingle(), Zz = reader.ReadSingle(), Zw = reader.ReadSingle(),
                Tx = reader.ReadSingle(), Ty = reader.ReadSingle(), Tz = reader.ReadSingle(), Tw = reader.ReadSingle()
            });
            Vector3 origin = GameBoxInterpreter.ToUnityPosition(transformMatrix.MultiplyPoint(Vector3.zero));

            var type = reader.ReadInt32();
            var customColorStringLength = reader.ReadInt32();

            uint tintColor = 0xffffffff;
            if (customColorStringLength > 0)
            {
                var parts = new string(reader.ReadChars(customColorStringLength)).Split(' ');
                if (parts.Length == 3)
                {
                    tintColor = 0xff000000 |
                                (uint.Parse(parts[0]) << 16) |
                                (uint.Parse(parts[1]) << 8) |
                                (uint.Parse(parts[2]));
                }
                else
                {
                    throw new Exception("Invalid TagNode color string.");
                }
            }

            return new TagNode()
            {
                Name = name,
                Origin = origin,
                TintColor = tintColor
            };
        }

        #if USE_UNSAFE_BINARY_READER
        private static PolMesh ReadMeshData(UnsafeBinaryReader reader, int version, int codepage)
        #else
        private static PolMesh ReadMeshData(BinaryReader reader, int version, int codepage)
        #endif
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
                    normals[i] = reader.ReadVector3();
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
                textureInfos[i] = ReadTextureInfo(reader, version, codepage);
            }

            return new PolMesh
            {
                BoundBox = boundBox,
                VertexFvfFlag = vertexTypeFlag,
                VertexInfo = vertexInfo,
                Textures = textureInfos
            };
        }

        #if USE_UNSAFE_BINARY_READER
        private static PolTexture ReadTextureInfo(UnsafeBinaryReader reader, int version, int codepage)
        #else
        private static PolTexture ReadTextureInfo(BinaryReader reader, int version, int codepage)
        #endif
        {
            var blendFlag = (GameBoxBlendFlag)reader.ReadUInt32();

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
                var textureName = reader.ReadString(64, codepage);
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