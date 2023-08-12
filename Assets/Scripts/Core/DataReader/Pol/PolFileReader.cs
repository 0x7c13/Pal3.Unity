// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
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

    public sealed class PolFileReader : IFileReader<PolFile>
    {
        private readonly int _codepage;

        public PolFileReader(int codepage)
        {
            _codepage = codepage;
        }

        public PolFile Read(IBinaryReader reader)
        {
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
                    Name     = reader.ReadString(32, _codepage),
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
                        tagNodes[i] = ReadTagNodeInfo(reader, _codepage);
                    }
                }
            }

            var meshInfos = new PolMesh[numberOfNodes];
            for (var i = 0; i < numberOfNodes; i++)
            {
                meshInfos[i] = ReadMeshData(reader, version, _codepage);
            }

            return new PolFile(version, nodeInfos, meshInfos, tagNodes);
        }

        private static TagNode ReadTagNodeInfo(IBinaryReader reader, int codepage)
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

        private static PolMesh ReadMeshData(IBinaryReader reader, int version, int codepage)
        {
            var bounds = new Bounds();
            bounds.SetMinMax(
                GameBoxInterpreter.ToUnityPosition(reader.ReadVector3()),
                GameBoxInterpreter.ToUnityPosition(reader.ReadVector3()));

            var vertexTypeFlag = reader.ReadUInt32();
            var numberOfVertices = reader.ReadInt32();

            if (numberOfVertices <= 0)
            {
                throw new InvalidDataException($"Invalid POLY(.pol) file: vertices == 0");
            }

            var positions = new Vector3[numberOfVertices];
            var normals = new Vector3[numberOfVertices];
            var diffuseColors = new Color32[numberOfVertices];
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
                    normals[i] = GameBoxInterpreter.ToUnityNormal(reader.ReadVector3());
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

                // Quick fix for the missing/wrong normals
                if (normals[i] == Vector3.zero) normals[i] = Vector3.up;
            }

            var vertexInfo = new PolVertexInfo()
            {
                Positions = positions,
                Normals = normals,
                DiffuseColors = diffuseColors,
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
                Bounds = bounds,
                VertexFvfFlag = vertexTypeFlag,
                VertexInfo = vertexInfo,
                Textures = textureInfos
            };
        }

        private static PolTexture ReadTextureInfo(IBinaryReader reader, int version, int codepage)
        {
            GameBoxBlendFlag blendFlag = (GameBoxBlendFlag)reader.ReadUInt32();

            GameBoxMaterial material = new ()
            {
                Diffuse = Utility.ToColor(reader.ReadSingles(4)),
                Ambient = Utility.ToColor(reader.ReadSingles(4)),
                Specular = Utility.ToColor(reader.ReadSingles(4)),
                Emissive = Utility.ToColor(reader.ReadSingles(4)),
                SpecularPower = reader.ReadSingle()
            };

            // Hack fix
            if (material.SpecularPower < 0) material.SpecularPower = 0;
            else if (material.SpecularPower > 128) material.SpecularPower = 128;

            var numberOfTextures = reader.ReadInt32();
            var textureNames = new string[numberOfTextures];

            for (var i = 0; i < numberOfTextures; i++)
            {
                var textureName = reader.ReadString(64, codepage);
                textureNames[i] = textureName;
            }

            material.TextureFileNames = textureNames;

            _ = reader.ReadInt32();
            var vertStart = reader.ReadUInt32();
            var vertEnd = reader.ReadUInt32();
            var numberOfFaces = reader.ReadInt32();

            var triangles = new int[numberOfFaces * 3];
            for (var i = 0; i < numberOfFaces; i++)
            {
                var index = i * 3;
                triangles[index] = reader.ReadUInt16();
                triangles[index + 1] = reader.ReadUInt16();
                triangles[index + 2] = reader.ReadUInt16();
            }

            GameBoxInterpreter.ToUnityTriangles(triangles);

            return new PolTexture()
            {
                BlendFlag = blendFlag,
                Material = material,
                VertStart = vertStart,
                VertEnd = vertEnd,
                Triangles = triangles
            };
        }
    }
}