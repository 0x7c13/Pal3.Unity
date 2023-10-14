// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Core.DataReader.Mv3
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Primitives;
    using Utilities;

    public sealed class Mv3FileReader : IFileReader<Mv3File>
    {
        public Mv3File Read(IBinaryReader reader, int codepage)
        {
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

            var materials = new GameBoxMaterial[numberOfMaterials];
            for (var i = 0; i < numberOfMaterials; i++)
            {
                materials[i] = ReadMaterial(reader, codepage);
            }

            var meshes = new Mv3Mesh[numberOfMeshes];
            for (var i = 0; i < numberOfMeshes; i++)
            {
                meshes[i] = ReadMesh(reader, codepage);
            }

            return new Mv3File(duration,
                animationEvents,
                tagNodes,
                meshes,
                materials);
        }

        private static Mv3Mesh ReadMesh(IBinaryReader reader, int codepage)
        {
            var name = reader.ReadString(64, codepage);
            var numberOfVertices = reader.ReadInt32();

            GameBoxVector3 gameBoxBoundsMin = reader.ReadVector3();
            GameBoxVector3 gameBoxBoundsMax = reader.ReadVector3();

            var numberOfFrames = reader.ReadInt32();
            var frames = new Mv3VertFrame[numberOfFrames];
            for (var i = 0; i < numberOfFrames; i++)
            {
                var gameBoxTick = reader.ReadUInt32();
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
                    GameBoxTick = gameBoxTick,
                    Vertices = vertices
                };
            }

            var numberOfTexCoords = reader.ReadInt32();
            GameBoxVector2[] texCoords;
            if (numberOfTexCoords == 0)
            {
                texCoords = new GameBoxVector2[] { new (0f, 0f) };
            }
            else
            {
                texCoords = new GameBoxVector2[numberOfTexCoords];
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

            return GetMv3Mesh(name, gameBoxBoundsMin, gameBoxBoundsMax, attributes, frames, texCoords);
        }

        private static Mv3Mesh GetMv3Mesh(string name,
            GameBoxVector3 gameBoxBoundsMin,
            GameBoxVector3 gameBoxBoundsMax,
            Mv3Attribute[] attributes,
            Mv3VertFrame[] vertFrames,
            GameBoxVector2[] texCoords)
        {
            int numberOfTriangles = attributes[0].IndexBuffers.Length * 3;
            var triangles = new int[numberOfTriangles];
            var uvs = new GameBoxVector2[numberOfTriangles];

            int totalVerticesPerKeyFrame = attributes[0].IndexBuffers.Length * 3;
            var keyFrameVertices = new GameBoxVector3[vertFrames.Length][];
            for (int i = 0; i < vertFrames.Length; i++)
            {
                keyFrameVertices[i] = new GameBoxVector3[totalVerticesPerKeyFrame];
            }

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

                        keyFrameVertices[k][triangleIndex] = new GameBoxVector3(vertex.X, vertex.Y, vertex.Z);
                    }

                    uvs[triangleIndex] = texCoords[indexBuffer.TexCoordIndex[j]];
                    triangles[triangleIndex] = triangleIndex;
                    triangleIndex++;
                }
            }

            var animationKeyFrames = new Mv3AnimationKeyFrame[vertFrames.Length];
            for (var i = 0; i < animationKeyFrames.Length; i++)
            {
                animationKeyFrames[i] = new Mv3AnimationKeyFrame()
                {
                    GameBoxTick = vertFrames[i].GameBoxTick,
                    GameBoxVertices = keyFrameVertices[i],
                };
            }

            return new Mv3Mesh
            {
                Name = name,
                GameBoxBoundsMin = gameBoxBoundsMin,
                GameBoxBoundsMax = gameBoxBoundsMax,
                Attributes = attributes,
                GameBoxTriangles = triangles,
                Uvs = uvs,
                GameBoxNormals = CoreUtility.CalculateNormals(animationKeyFrames[0].GameBoxVertices, triangles),
                KeyFrames = animationKeyFrames,
            };
        }

        private static Mv3TagNode ReadTagNode(IBinaryReader reader, int codepage)
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

        private static Mv3TagFrame ReadTagFrame(IBinaryReader reader)
        {
            var gameBoxTick = reader.ReadUInt32();
            GameBoxVector3 gameBoxPosition = reader.ReadVector3();

            var gameBoxRotation = new GameBoxQuaternion()
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
                GameBoxTick = gameBoxTick,
                GameBoxPosition = gameBoxPosition,
                GameBoxRotation = gameBoxRotation,
                Scale = scale,
            };
        }

        private static Mv3AnimationEvent ReadAnimationEvent(IBinaryReader reader, int codepage)
        {
            return new Mv3AnimationEvent()
            {
                GameBoxTick = reader.ReadUInt32(),
                Name = reader.ReadString(16, codepage),
            };
        }

        private static GameBoxMaterial ReadMaterial(IBinaryReader reader, int codepage)
        {
            GameBoxMaterial material = new ()
            {
                Diffuse = CoreUtility.ToColor(reader.ReadSingles(4)),
                Ambient = CoreUtility.ToColor(reader.ReadSingles(4)),
                Specular = CoreUtility.ToColor(reader.ReadSingles(4)),
                Emissive = CoreUtility.ToColor(reader.ReadSingles(4)),
                SpecularPower = reader.ReadSingle()
            };

            List<string> textureNames = new ();
            for (var i = 0; i < 4; i++)
            {
                string textureName;
                var length = reader.ReadInt32();

                if (length == 0)
                {
                    if (i > 0) continue;
                    textureName = string.Empty; // Use default white texture
                }
                else
                {
                    textureName = reader.ReadString(length, codepage);
                }

                textureNames.Add(textureName);
            }

            material.TextureFileNames = textureNames.ToArray();

            return material;
        }
    }
}