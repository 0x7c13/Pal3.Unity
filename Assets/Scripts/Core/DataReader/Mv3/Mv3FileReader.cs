// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE.txt in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Core.DataReader.Mv3
{
    using System.Collections.Generic;
    using System.IO;
    using Extensions;
    using GameBox;
    using UnityEngine;
    using Utils;

    public static class Mv3FileReader
    {
        private const int ACTOR_NODE_NAME_MAX = 64;
        private const int ACTOR_ANIMATION_EVENT_NAME_MAX = 16;

        public static Mv3File Read(Stream stream)
        {
            using var reader = new BinaryReader(stream);

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

            var animationEvents = new List<Mv3AnimationEvent>();
            for (var i = 0; i < numberOfAnimationEvents; i++)
            {
                animationEvents.Add(ReadAnimationEvent(reader));
            }

            var tagNodes = new List<Mv3TagNode>();
            for (var i = 0; i < numberOfTagNodes; i++)
            {
                tagNodes.Add(ReadTagNode(reader));
            }

            var materials = new List<Mv3Material>();
            for (var i = 0; i < numberOfMaterials; i++)
            {
                materials.Add(ReadMaterial(reader));
            }

            var meshes = new List<Mv3Mesh>();
            for (var i = 0; i < numberOfMeshes; i++)
            {
                meshes.Add(ReadMesh(reader));
            }

            return new Mv3File(version,
                duration,
                animationEvents.ToArray(),
                tagNodes.ToArray(),
                materials.ToArray(),
                meshes.ToArray());
        }

        private static Mv3Mesh ReadMesh(BinaryReader reader)
        {
            var name = reader.ReadGbkString(ACTOR_NODE_NAME_MAX);
            var numberOfVertices = reader.ReadInt32();
            var boundBox = new GameBoxAABBox()
            {
                Min = reader.ReadVector3(),
                Max = reader.ReadVector3()
            };
            var numberOfFrames = reader.ReadInt32();
            var frames = new List<Mv3VertFrame>();
            for (var i = 0; i < numberOfFrames; i++)
            {
                var tick = reader.ReadUInt32();
                var vertices = new List<Mv3Vert>();
                for (var j = 0; j < numberOfVertices; j++)
                {
                    vertices.Add(new Mv3Vert()
                    {
                        X = reader.ReadInt16(),
                        Y = reader.ReadInt16(),
                        Z = reader.ReadInt16(),
                        N = reader.ReadUInt16()
                    });
                }
                frames.Add(new Mv3VertFrame()
                {
                    Tick = tick,
                    Vertices = vertices.ToArray()
                });
            }

            var numberOfTexCoords = reader.ReadInt32();
            var texCoords = new List<Vector2>();
            if (numberOfTexCoords == 0)
            {
                texCoords.Add(new Vector2(0f, 0f));
                Debug.LogWarning("numberOfTexCoords == 0");
            }
            else
            {
                for (var i = 0; i < numberOfTexCoords; i++)
                {
                    texCoords.Add(reader.ReadVector2());
                }
            }

            var numberOfAttributes = reader.ReadInt32();
            var attributes = new List<Mv3Attribute>();
            for (var i = 0; i < numberOfAttributes; i++)
            {
                var materialId = reader.ReadInt32();
                var numberOfTriangles = reader.ReadInt32();

                var triangles = new List<Mv3IndexBuffer>();
                for (int j = 0; j < numberOfTriangles; j++)
                {
                    triangles.Add(new Mv3IndexBuffer()
                    {
                        TriangleIndex = new []{ reader.ReadUInt16(), reader.ReadUInt16(), reader.ReadUInt16()},
                        TexCoordIndex = new []{ reader.ReadUInt16(), reader.ReadUInt16(), reader.ReadUInt16()}
                    });
                }

                var numberOfCommands = reader.ReadInt32();
                var commands = new List<int>();
                for (int j = 0; j < numberOfCommands; j++)
                {
                    commands.Add(reader.ReadInt32());
                }

                attributes.Add(new Mv3Attribute()
                {
                    MaterialId = materialId,
                    IndexBuffers = triangles.ToArray(),
                    Commands = commands.ToArray(),
                });
            }

            return new Mv3Mesh()
            {
                Name = name,
                BoundBox = boundBox,
                Attributes = attributes.ToArray(),
                Frames = frames.ToArray(),
                TexCoords = texCoords.ToArray(),
            };
        }

        private static Mv3TagNode ReadTagNode(BinaryReader reader)
        {
            var nodeName = reader.ReadGbkString(ACTOR_NODE_NAME_MAX);
            var flipScale = reader.ReadSingle();
            var numberOfFrames = reader.ReadInt32();

            var tagFrames = new List<Mv3TagFrame>();
            for (var j = 0; j < numberOfFrames; j++)
            {
                tagFrames.Add(ReadTagFrame(reader));
            }

            return new Mv3TagNode()
            {
                Name = nodeName,
                TagFrames = tagFrames.ToArray(),
                FlipScale = flipScale
            };
        }

        private static Mv3TagFrame ReadTagFrame(BinaryReader reader)
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

        private static Mv3AnimationEvent ReadAnimationEvent(BinaryReader reader)
        {
            var tick = reader.ReadUInt32();
            var name = reader.ReadBytes(ACTOR_ANIMATION_EVENT_NAME_MAX);

            return new Mv3AnimationEvent()
            {
                Tick = tick,
                Name = Utility.ConvertToGbkString(name)
            };
        }

        private static Mv3Material ReadMaterial(BinaryReader reader)
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
                    textureName = "GB_DEFAULT_WHITE_TEXTURE"; // Use default white texture
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