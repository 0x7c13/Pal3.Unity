﻿// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

#define USE_UNSAFE_BINARY_READER

namespace Core.DataReader.Mv3
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Extensions;
    using GameBox;
    using UnityEngine;
    using Utils;

    public static class Mv3FileReader
    {
        public static Mv3File Read(byte[] data, int codepage)
        {
            #if USE_UNSAFE_BINARY_READER
            using var reader = new UnsafeBinaryReader(data);
            #else
            using var stream = new MemoryStream(data);
            using var reader = new BinaryReader(stream);
            #endif

            var header = reader.ReadChars(4);
            var headerStr = new string(header[..^1]);
            
            if (headerStr != "MV3")
            {
                throw new InvalidDataException("Invalid MV3(.mv3) file: header != MV3");
            }

            var version = reader.ReadInt32();
            if (version != 100)
            {
                throw new InvalidDataException("Invalid MV3(.mv3) file: version != 100");
            }

            var duration = reader.ReadUInt32();
            var numberOfMaterials = reader.ReadInt32();
            var numberOfTagNodes = reader.ReadInt32();
            var numberOfMeshes = reader.ReadInt32();
            var numberOfAnimationEvents = reader.ReadInt32();

            if (numberOfMeshes == 0 || numberOfMaterials == 0)
            {
                throw new InvalidDataException("Invalid MV3(.mv3) file: missing mesh or material info");
            }

            var animationEvents = new Mv3AnimationEvent[numberOfAnimationEvents];
            for (var i = 0; i < numberOfAnimationEvents; i++)
            {
                animationEvents[i] = ReadAnimationEvent(reader, codepage);
            }

            var tagNodes = new Mv3TagNode[numberOfTagNodes];
            for (var i = 0; i < numberOfTagNodes; i++)
            {
                tagNodes[i] = ReadTagNode(reader, codepage);
            }

            var materials = new Mv3Material[numberOfMaterials];
            for (var i = 0; i < numberOfMaterials; i++)
            {
                materials[i] = ReadMaterial(reader, codepage);
            }

            var meshes = new Mv3Mesh[numberOfMeshes];
            for (var i = 0; i < numberOfMeshes; i++)
            {
                meshes[i] = ReadMesh(reader, codepage);
            }

            return new Mv3File(version,
                duration,
                animationEvents,
                tagNodes,
                materials,
                meshes);
        }

        #if USE_UNSAFE_BINARY_READER
        private static Mv3Mesh ReadMesh(UnsafeBinaryReader reader, int codepage)
        #else
        private static Mv3Mesh ReadMesh(BinaryReader reader, int codepage)
        #endif
        {
            var name = reader.ReadString(64, codepage);
            var numberOfVertices = reader.ReadInt32();
            var boundBox = new GameBoxAABBox()
            {
                Min = reader.ReadVector3(),
                Max = reader.ReadVector3()
            };
            var numberOfFrames = reader.ReadInt32();
            var frames = new Mv3VertFrame[numberOfFrames];
            for (var i = 0; i < numberOfFrames; i++)
            {
                var tick = reader.ReadUInt32();
                var vertices = new Mv3Vert[numberOfVertices];
                for (var j = 0; j < numberOfVertices; j++)
                {
                    vertices[j] = new Mv3Vert()
                    {
                        X = reader.ReadInt16(),
                        Y = reader.ReadInt16(),
                        Z = reader.ReadInt16(),
                        N = reader.ReadUInt16()
                    };
                }
                frames[i] = new Mv3VertFrame()
                {
                    Tick = tick,
                    Vertices = vertices
                };
            }

            var numberOfTexCoords = reader.ReadInt32();
            Vector2[] texCoords;
            if (numberOfTexCoords == 0)
            {
                texCoords = new Vector2[] { new (0f, 0f) };
                Debug.LogWarning("numberOfTexCoords == 0");
            }
            else
            {
                texCoords = new Vector2[numberOfTexCoords];
                for (var i = 0; i < numberOfTexCoords; i++)
                {
                    texCoords[i] = reader.ReadVector2();
                }
            }

            var numberOfAttributes = reader.ReadInt32();
            var attributes = new Mv3Attribute[numberOfAttributes];
            for (var i = 0; i < numberOfAttributes; i++)
            {
                var materialId = reader.ReadInt32();
                var numberOfTriangles = reader.ReadInt32();

                var triangles = new Mv3IndexBuffer[numberOfTriangles];
                for (var j = 0; j < numberOfTriangles; j++)
                {
                    triangles[j] = new Mv3IndexBuffer()
                    {
                        TriangleIndex = new []{ reader.ReadUInt16(), reader.ReadUInt16(), reader.ReadUInt16()},
                        TexCoordIndex = new []{ reader.ReadUInt16(), reader.ReadUInt16(), reader.ReadUInt16()}
                    };
                }

                var numberOfCommands = reader.ReadInt32();
                var commands = new int[numberOfCommands];
                for (var j = 0; j < numberOfCommands; j++)
                {
                    commands[j] = reader.ReadInt32();
                }

                attributes[i] = new Mv3Attribute()
                {
                    MaterialId = materialId,
                    IndexBuffers = triangles,
                    Commands = commands,
                };
            }
            
            return GetMv3Mesh(name, boundBox, attributes, frames, texCoords);
        }

        private static Mv3Mesh GetMv3Mesh(string name,
            GameBoxAABBox boundBox,
            Mv3Attribute[] attributes,
            Mv3VertFrame[] vertFrames,
            Vector2[] texCoords)
        {
            var triangles = new int[attributes[0].IndexBuffers.Length * 3];
            var keyFrameVertices = new List<Vector3>[vertFrames.Length]
                .Select(item=>new List<Vector3>()).ToArray();
            var uvs = new Vector2[attributes[0].IndexBuffers.Length * 3];

            var triangleIndex = 0;
            
            for (var i = 0; i < attributes[0].IndexBuffers.Length; i++)
            {
                Mv3IndexBuffer indexBuffer = attributes[0].IndexBuffers[i];
                for (var j = 0; j < 3; j++)
                {
                    for (var k = 0; k < vertFrames.Length; k++)
                    {
                        Mv3VertFrame frame = vertFrames[k];
                        Mv3Vert vertex = frame.Vertices[indexBuffer.TriangleIndex[j]];

                        keyFrameVertices[k].Add((GameBoxInterpreter
                            .ToUnityVertex(new Vector3(vertex.X, vertex.Y, vertex.Z),
                                GameBoxInterpreter.GameBoxMv3UnitToUnityUnit)));
                    }

                    uvs[triangleIndex] = texCoords[indexBuffer.TexCoordIndex[j]];
                    triangles[triangleIndex] = triangleIndex;
                    triangleIndex++;
                }
            }

            GameBoxInterpreter.ToUnityTriangles(triangles);

            var animationKeyFrames = new VertexAnimationKeyFrame[vertFrames.Length];
            for (var i = 0; i < animationKeyFrames.Length; i++)
            {
                animationKeyFrames[i] = new VertexAnimationKeyFrame()
                {
                    Tick = vertFrames[i].Tick,
                    Vertices = keyFrameVertices[i].ToArray(),
                };
            }
            
            return new Mv3Mesh
            {
                Name = name,
                BoundBox = boundBox,
                Attributes = attributes,
                Triangles = triangles,
                Uvs = uvs,
                Normals = Utility.CalculateNormals(animationKeyFrames[0].Vertices, triangles),
                KeyFrames = animationKeyFrames,
            };
        }

        #if USE_UNSAFE_BINARY_READER
        private static Mv3TagNode ReadTagNode(UnsafeBinaryReader reader, int codepage)
        #else
        private static Mv3TagNode ReadTagNode(BinaryReader reader, int codepage)
        #endif
        {
            var nodeName = reader.ReadString(64, codepage);
            var flipScale = reader.ReadSingle();
            var numberOfFrames = reader.ReadInt32();

            var tagFrames = new Mv3TagFrame[numberOfFrames];
            for (var i = 0; i < numberOfFrames; i++)
            {
                tagFrames[i] = ReadTagFrame(reader);
            }

            return new Mv3TagNode()
            {
                Name = nodeName,
                TagFrames = tagFrames,
                FlipScale = flipScale
            };
        }

        #if USE_UNSAFE_BINARY_READER
        private static Mv3TagFrame ReadTagFrame(UnsafeBinaryReader reader)
        #else
        private static Mv3TagFrame ReadTagFrame(BinaryReader reader)
        #endif
        {
            var tick = reader.ReadUInt32();
            Vector3 position = GameBoxInterpreter.ToUnityPosition(reader.ReadVector3());

            var rotation = GameBoxInterpreter.Mv3QuaternionToUnityQuaternion(new GameBoxQuaternion()
            {
                X = reader.ReadSingle(),
                Y = reader.ReadSingle(),
                Z = reader.ReadSingle(),
                W = reader.ReadSingle(),
            });

            var scale = new []
            {
                new [] {reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()},
                new [] {reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()},
                new [] {reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()},
            };

            return new Mv3TagFrame()
            {
                Tick = tick,
                Position = position,
                Rotation = rotation,
                Scale = scale,
            };
        }

        #if USE_UNSAFE_BINARY_READER
        private static Mv3AnimationEvent ReadAnimationEvent(UnsafeBinaryReader reader, int codepage)
        #else
        private static Mv3AnimationEvent ReadAnimationEvent(BinaryReader reader, int codepage)
        #endif
        {
            var tick = reader.ReadUInt32();
            var name = reader.ReadBytes(16);

            return new Mv3AnimationEvent()
            {
                Tick = tick,
                Name = Utility.ConvertToString(name, codepage)
            };
        }

        #if USE_UNSAFE_BINARY_READER
        private static Mv3Material ReadMaterial(UnsafeBinaryReader reader, int codepage)
        #else
        private static Mv3Material ReadMaterial(BinaryReader reader, int codepage)
        #endif
        {
            var material = new GameBoxMaterial()
            {
                Diffuse = Utility.ToColor(reader.ReadSingleArray(4)),
                Ambient = Utility.ToColor(reader.ReadSingleArray(4)),
                Specular = Utility.ToColor(reader.ReadSingleArray(4)),
                Emissive = Utility.ToColor(reader.ReadSingleArray(4)),
                Power = reader.ReadSingle()
            };

            var textureNames = new List<string>();
            for (var i = 0; i < 4; i++)
            {
                string textureName;
                var length = reader.ReadInt32();
                if (length is < 0 or > 255)
                {
                    throw new InvalidDataException($"Invalid length of material name: {length}");
                }

                if (length == 0)
                {
                    if (i > 0) continue;
                    textureName = string.Empty; // Use default white texture?
                }
                else
                {
                    textureName = reader.ReadString(length, codepage);
                }

                textureNames.Add(textureName);
            }

            return new Mv3Material()
            {
                Material = material,
                TextureNames = textureNames.ToArray(),
            };
        }
    }
}