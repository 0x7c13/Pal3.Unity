// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2025, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Core.DataReader.Pol
{
    using System;
    using System.IO;
    using Primitives;
    using Utilities;

    public sealed class PolFileReader : IFileReader<PolFile>
    {
        public PolFile Read(IBinaryReader reader, int codepage)
        {
            char[] header = reader.ReadChars(4);
            string headerStr = new string(header);

            if (headerStr != "POLY")
            {
                throw new InvalidDataException($"Invalid POLY(.pol) file: header != POLY");
            }

            int version = reader.ReadInt32();
            int numberOfNodes = reader.ReadInt32();

            PolGeometryNode[] nodeInfos = new PolGeometryNode[numberOfNodes];
            for (int i = 0; i < numberOfNodes; i++)
            {
                nodeInfos[i] = new PolGeometryNode
                {
                    Name     = reader.ReadString(32, codepage),
                    GameBoxPosition = reader.ReadGameBoxVector3(),
                    Radius   = reader.ReadSingle(),
                    Offset   = reader.ReadInt32()
                };
            }

            TagNode[] tagNodes = Array.Empty<TagNode>();
            if (version > 100)
            {
                int numberOfTagNodes = reader.ReadInt32();
                if (numberOfTagNodes > 0)
                {
                    tagNodes = new TagNode[numberOfTagNodes];
                    for (int i = 0; i < numberOfTagNodes; i++)
                    {
                        tagNodes[i] = ReadTagNodeInfo(reader, codepage);
                    }
                }
            }

            PolMesh[] meshInfos = new PolMesh[numberOfNodes];
            for (int i = 0; i < numberOfNodes; i++)
            {
                meshInfos[i] = ReadMeshData(reader, version, codepage);
            }

            return new PolFile(version, nodeInfos, meshInfos, tagNodes);
        }

        private static TagNode ReadTagNodeInfo(IBinaryReader reader, int codepage)
        {
            string name = reader.ReadString(32, codepage);
            GameBoxMatrix4x4 gameBoxTransformMatrix = new GameBoxMatrix4x4()
            {
                Xx = reader.ReadSingle(), Xy = reader.ReadSingle(), Xz = reader.ReadSingle(), Xw = reader.ReadSingle(),
                Yx = reader.ReadSingle(), Yy = reader.ReadSingle(), Yz = reader.ReadSingle(), Yw = reader.ReadSingle(),
                Zx = reader.ReadSingle(), Zy = reader.ReadSingle(), Zz = reader.ReadSingle(), Zw = reader.ReadSingle(),
                Tx = reader.ReadSingle(), Ty = reader.ReadSingle(), Tz = reader.ReadSingle(), Tw = reader.ReadSingle()
            };

            int type = reader.ReadInt32();
            int customColorStringLength = reader.ReadInt32();

            uint tintColor = 0xffffffff;
            if (customColorStringLength > 0)
            {
                string[] parts = new string(reader.ReadChars(customColorStringLength)).Split(' ');
                if (parts.Length == 3)
                {
                    tintColor = 0xff000000 |
                                (uint.Parse(parts[0]) << 16) |
                                (uint.Parse(parts[1]) << 8) |
                                (uint.Parse(parts[2]));
                }
                else
                {
                    throw new InvalidDataException("Invalid TagNode color string");
                }
            }

            return new TagNode()
            {
                Name = name,
                GameBoxTransformMatrix = gameBoxTransformMatrix,
                TintColor = tintColor
            };
        }

        private static PolMesh ReadMeshData(IBinaryReader reader, int version, int codepage)
        {
            GameBoxVector3 gameBoxBoundsMin = reader.ReadGameBoxVector3();
            GameBoxVector3 gameBoxBoundsMax = reader.ReadGameBoxVector3();

            uint vertexTypeFlag = reader.ReadUInt32();
            int numberOfVertices = reader.ReadInt32();

            if (numberOfVertices <= 0)
            {
                throw new InvalidDataException($"Invalid POLY(.pol) file: vertices == 0");
            }

            GameBoxVector3[] gameBoxPositions = new GameBoxVector3[numberOfVertices];
            GameBoxVector3[] gameBoxNormals = new GameBoxVector3[numberOfVertices];
            Color32[] diffuseColors = new Color32[numberOfVertices];
            Color32[] specularColors = new Color32[numberOfVertices];
            GameBoxVector2[][] uvs = new GameBoxVector2[4][];

            uvs[0] = new GameBoxVector2[numberOfVertices];
            uvs[1] = new GameBoxVector2[numberOfVertices];
            uvs[2] = new GameBoxVector2[numberOfVertices];
            uvs[3] = new GameBoxVector2[numberOfVertices];

            for (int i = 0; i < numberOfVertices; i++)
            {
                if ((vertexTypeFlag & GameBoxVertexType.XYZ) != 0)
                {
                    gameBoxPositions[i] = reader.ReadGameBoxVector3();
                }
                if ((vertexTypeFlag & GameBoxVertexType.XYZRHW) != 0)
                {
                    gameBoxPositions[i] = reader.ReadGameBoxVector3();
                    _ = reader.ReadSingle();
                }
                if ((vertexTypeFlag & GameBoxVertexType.Normal) != 0)
                {
                    gameBoxNormals[i] = reader.ReadGameBoxVector3();
                }
                if ((vertexTypeFlag & GameBoxVertexType.Diffuse) != 0)
                {
                    diffuseColors[i] = reader.ReadColor32();
                }
                if ((vertexTypeFlag & GameBoxVertexType.Specular) != 0)
                {
                    specularColors[i] = reader.ReadColor32();
                }
                if ((vertexTypeFlag & GameBoxVertexType.UV0) != 0)
                {
                    float x = reader.ReadSingle();
                    float y = reader.ReadSingle();
                    uvs[0][i] = new GameBoxVector2(x, y);
                }
                if ((vertexTypeFlag & GameBoxVertexType.UV1) != 0)
                {
                    float x = reader.ReadSingle();
                    float y = reader.ReadSingle();
                    uvs[1][i] = new GameBoxVector2(x, y);
                }
                if ((vertexTypeFlag & GameBoxVertexType.UV2) != 0)
                {
                    float x = reader.ReadSingle();
                    float y = reader.ReadSingle();
                    uvs[2][i] = new GameBoxVector2(x, y);
                }
                if ((vertexTypeFlag & GameBoxVertexType.UV3) != 0)
                {
                    float x = reader.ReadSingle();
                    float y = reader.ReadSingle();
                    uvs[3][i] = new GameBoxVector2(x, y);
                }

                // Quick fix for the missing/wrong normals
                if (gameBoxNormals[i] == GameBoxVector3.Zero)
                {
                    gameBoxNormals[i] = new GameBoxVector3(0, 1, 0); // Up
                }
            }

            PolVertexInfo vertexInfo = new()
            {
                GameBoxPositions = gameBoxPositions,
                GameBoxNormals = gameBoxNormals,
                DiffuseColors = diffuseColors,
                SpecularColors = specularColors,
                Uvs = uvs,
            };

            int numberOfTextures = reader.ReadInt32();

            PolTexture[] textureInfos = new PolTexture[numberOfTextures];

            for (int i = 0; i < numberOfTextures; i++)
            {
                textureInfos[i] = ReadTextureInfo(reader, version, codepage);
            }

            return new PolMesh
            {
                GameBoxBoundsMin = gameBoxBoundsMin,
                GameBoxBoundsMax = gameBoxBoundsMax,
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
                Diffuse = reader.ReadColor(),
                Ambient = reader.ReadColor(),
                Specular = reader.ReadColor(),
                Emissive = reader.ReadColor(),
                SpecularPower = reader.ReadSingle()
            };

            // Hack fix
            if (material.SpecularPower < 0) material.SpecularPower = 0;
            else if (material.SpecularPower > 128) material.SpecularPower = 128;

            int numberOfTextures = reader.ReadInt32();
            string[] textureNames = new string[numberOfTextures];

            for (int i = 0; i < numberOfTextures; i++)
            {
                string textureName = reader.ReadString(64, codepage);
                textureNames[i] = textureName;
            }

            material.TextureFileNames = textureNames;

            _ = reader.ReadInt32();
            uint vertStart = reader.ReadUInt32();
            uint vertEnd = reader.ReadUInt32();
            int numberOfFaces = reader.ReadInt32();

            int[] gameBoxTriangles = new int[numberOfFaces * 3];
            for (int i = 0; i < numberOfFaces; i++)
            {
                int index = i * 3;
                gameBoxTriangles[index] = reader.ReadUInt16();
                gameBoxTriangles[index + 1] = reader.ReadUInt16();
                gameBoxTriangles[index + 2] = reader.ReadUInt16();
            }

            return new PolTexture()
            {
                BlendFlag = blendFlag,
                Material = material,
                VertStart = vertStart,
                VertEnd = vertEnd,
                GameBoxTriangles = gameBoxTriangles
            };
        }
    }
}