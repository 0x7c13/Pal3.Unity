// ---------------------------------------------------------------------------------------------
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
        public static Mv3File Read(byte[] data)
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
                animationEvents[i] = ReadAnimationEvent(reader);
            }

            var tagNodes = new Mv3TagNode[numberOfTagNodes];
            for (var i = 0; i < numberOfTagNodes; i++)
            {
                tagNodes[i] = ReadTagNode(reader);
            }

            var materials = new Mv3Material[numberOfMaterials];
            for (var i = 0; i < numberOfMaterials; i++)
            {
                materials[i] = ReadMaterial(reader);
            }

            var meshes = new Mv3Mesh[numberOfMeshes];
            var meshKeyFrames = new VertexAnimationKeyFrame[numberOfMeshes][];
            for (var i = 0; i < numberOfMeshes; i++)
            {
                var mesh = ReadMesh(reader);
                meshes[i] = mesh;
                meshKeyFrames[i] = CalculateKeyFrameVertices(mesh);
            }

            return new Mv3File(version,
                duration,
                animationEvents,
                tagNodes,
                materials,
                meshes,
                meshKeyFrames);
        }

        #if USE_UNSAFE_BINARY_READER
        private static Mv3Mesh ReadMesh(UnsafeBinaryReader reader)
        #else
        private static Mv3Mesh ReadMesh(BinaryReader reader)
        #endif
        {
            var name = reader.ReadGbkString(64);
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
                texCoords = new Vector2[] {new (0f, 0f)};
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

            return new Mv3Mesh()
            {
                Name = name,
                BoundBox = boundBox,
                Attributes = attributes,
                Frames = frames,
                TexCoords = texCoords,
            };
        }

        private static VertexAnimationKeyFrame[] CalculateKeyFrameVertices(Mv3Mesh mv3Mesh)
        {
            var triangles = new List<int>();
            var texCoords = mv3Mesh.TexCoords;
            var indexMap = new Dictionary<int, int>();
            var keyFrameInfo = new List<(Vector3 vertex, Vector2 uv)>[mv3Mesh.Frames.Length]
                .Select(item=>new List<(Vector3 vertex, Vector2 uv)>()).ToArray();

            for (var i = 0; i < mv3Mesh.Attributes[0].IndexBuffers.Length; i++)
            {
                var indexBuffer = mv3Mesh.Attributes[0].IndexBuffers[i];
                for (var j = 0; j < 3; j++)
                {
                    var hash = indexBuffer.TriangleIndex[j] * texCoords.Length + indexBuffer.TexCoordIndex[j];
                    if (indexMap.ContainsKey(hash))
                    {
                        triangles.Add(indexMap[hash]);
                    }
                    else
                    {
                        var index = indexMap.Keys.Count;

                        for (var k = 0; k < mv3Mesh.Frames.Length; k++)
                        {
                            var frame = mv3Mesh.Frames[k];
                            var vertex = frame.Vertices[indexBuffer.TriangleIndex[j]];

                            keyFrameInfo[k].Add((GameBoxInterpreter
                                .ToUnityVertex(new Vector3(vertex.X, vertex.Y, vertex.Z),
                                    GameBoxInterpreter.GameBoxMv3UnitToUnityUnit),
                                texCoords[indexBuffer.TexCoordIndex[j]]));
                        }

                        indexMap[hash] = index;
                        triangles.Add(index);
                    }
                }
            }

            GameBoxInterpreter.ToUnityTriangles(triangles);

            var animationKeyFrames = new VertexAnimationKeyFrame[mv3Mesh.Frames.Length];
            for (var i = 0; i < animationKeyFrames.Length; i++)
            {
                animationKeyFrames[i] = new VertexAnimationKeyFrame()
                {
                    Tick = mv3Mesh.Frames[i].Tick,
                    Vertices = keyFrameInfo[i].Select(f => f.vertex).ToArray(),
                    Triangles = triangles.ToArray(),
                    Uv = keyFrameInfo[i].Select(f => f.uv).ToArray(),
                };
            }

            return animationKeyFrames;
        }

        #if USE_UNSAFE_BINARY_READER
        private static Mv3TagNode ReadTagNode(UnsafeBinaryReader reader)
        #else
        private static Mv3TagNode ReadTagNode(BinaryReader reader)
        #endif
        {
            var nodeName = reader.ReadGbkString(64);
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
            var position = reader.ReadVector3();

            var rotation = new GameBoxQuaternion()
            {
                X = reader.ReadSingle(),
                Y = reader.ReadSingle(),
                Z = reader.ReadSingle(),
                W = reader.ReadSingle(),
            };

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
        private static Mv3AnimationEvent ReadAnimationEvent(UnsafeBinaryReader reader)
        #else
        private static Mv3AnimationEvent ReadAnimationEvent(BinaryReader reader)
        #endif
        {
            var tick = reader.ReadUInt32();
            var name = reader.ReadBytes(16);

            return new Mv3AnimationEvent()
            {
                Tick = tick,
                Name = Utility.ConvertToGbkString(name)
            };
        }

        #if USE_UNSAFE_BINARY_READER
        private static Mv3Material ReadMaterial(UnsafeBinaryReader reader)
        #else
        private static Mv3Material ReadMaterial(BinaryReader reader)
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
                    textureName = reader.ReadGbkString(length);
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