// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2025, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Core.DataReader.Mv3
{
    using System.Collections.Generic;
    using System.IO;
    using Primitives;
    using Utilities;

    public sealed class Mv3FileReader : IFileReader<Mv3File>
    {
        public Mv3File Read(IBinaryReader reader, int codepage)
        {
            char[] header = reader.ReadChars(4);
            string headerStr = new string(header[..^1]);

            if (headerStr != "MV3")
            {
                throw new InvalidDataException("Invalid MV3(.mv3) file: header != MV3");
            }

            int version = reader.ReadInt32();
            if (version != 100)
            {
                throw new InvalidDataException("Invalid MV3(.mv3) file: version != 100");
            }

            uint duration = reader.ReadUInt32();
            int numberOfMaterials = reader.ReadInt32();
            int numberOfTagNodes = reader.ReadInt32();
            int numberOfMeshes = reader.ReadInt32();
            int numberOfAnimationEvents = reader.ReadInt32();

            if (numberOfMeshes == 0 || numberOfMaterials == 0)
            {
                throw new InvalidDataException("Invalid MV3(.mv3) file: missing mesh or material info");
            }

            Mv3AnimationEvent[] animationEvents = new Mv3AnimationEvent[numberOfAnimationEvents];
            for (int i = 0; i < numberOfAnimationEvents; i++)
            {
                animationEvents[i] = ReadAnimationEvent(reader, codepage);
            }

            Mv3TagNode[] tagNodes = new Mv3TagNode[numberOfTagNodes];
            for (int i = 0; i < numberOfTagNodes; i++)
            {
                tagNodes[i] = ReadTagNode(reader, codepage);
            }

            GameBoxMaterial[] materials = new GameBoxMaterial[numberOfMaterials];
            for (int i = 0; i < numberOfMaterials; i++)
            {
                materials[i] = ReadMaterial(reader, codepage);
            }

            Mv3Mesh[] meshes = new Mv3Mesh[numberOfMeshes];
            for (int i = 0; i < numberOfMeshes; i++)
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
            string name = reader.ReadString(64, codepage);
            int numberOfVertices = reader.ReadInt32();

            GameBoxVector3 gameBoxBoundsMin = reader.ReadGameBoxVector3();
            GameBoxVector3 gameBoxBoundsMax = reader.ReadGameBoxVector3();

            int numberOfFrames = reader.ReadInt32();
            Mv3VertFrame[] frames = new Mv3VertFrame[numberOfFrames];
            for (int i = 0; i < numberOfFrames; i++)
            {
                uint gameBoxTick = reader.ReadUInt32();
                Mv3Vert[] vertices = new Mv3Vert[numberOfVertices];
                for (int j = 0; j < numberOfVertices; j++)
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

            int numberOfTexCoords = reader.ReadInt32();
            GameBoxVector2[] texCoords = numberOfTexCoords == 0 ?
                new GameBoxVector2[] { new (0f, 0f) } :
                reader.ReadGameBoxVector2s(numberOfTexCoords);

            int numberOfAttributes = reader.ReadInt32();
            Mv3Attribute[] attributes = new Mv3Attribute[numberOfAttributes];
            for (int i = 0; i < numberOfAttributes; i++)
            {
                int materialId = reader.ReadInt32();
                int numberOfTriangles = reader.ReadInt32();

                Mv3IndexBuffer[] triangles = new Mv3IndexBuffer[numberOfTriangles];
                for (int j = 0; j < numberOfTriangles; j++)
                {
                    triangles[j] = new Mv3IndexBuffer()
                    {
                        TriangleIndex = new []{ reader.ReadUInt16(), reader.ReadUInt16(), reader.ReadUInt16()},
                        TexCoordIndex = new []{ reader.ReadUInt16(), reader.ReadUInt16(), reader.ReadUInt16()}
                    };
                }

                int numberOfCommands = reader.ReadInt32();
                int[] commands = new int[numberOfCommands];
                for (int j = 0; j < numberOfCommands; j++)
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
            Mv3IndexBuffer[] faces = attributes[0].IndexBuffers;
            int[] triangles = new int[faces.Length * 3];
            int positionCount = vertFrames[0].Vertices.Length;
            var sourceCorners = new int[triangles.Length];
            for (int i = 0; i < faces.Length; i++)
            {
                ushort[] corners = faces[i].TriangleIndex;
                sourceCorners[i * 3] = corners[0];
                sourceCorners[i * 3 + 1] = corners[1];
                sourceCorners[i * 3 + 2] = corners[2];
            }

            var (normalCorners, normalCount) = CoreUtility.GetNormalIndices(sourceCorners, positionCount);
            var renderVertexByCorner = new Dictionary<(int normalIndex, ushort uv), int>();
            var normalIndices = new List<int>();
            var positionIndices = new List<int>();
            var uvs = new List<GameBoxVector2>();

            // Share render vertices across matching corners while keeping UV seams and back faces distinct.
            int corner = 0;
            foreach (Mv3IndexBuffer face in faces)
            {
                for (int j = 0; j < 3; j++)
                {
                    int normalIndex = normalCorners[corner];
                    var key = (normalIndex, uv: face.TexCoordIndex[j]);
                    if (!renderVertexByCorner.TryGetValue(key, out int renderVertex))
                    {
                        renderVertex = normalIndices.Count;
                        renderVertexByCorner.Add(key, renderVertex);
                        normalIndices.Add(normalIndex);
                        positionIndices.Add(face.TriangleIndex[j]);
                        uvs.Add(texCoords[key.uv]);
                    }

                    triangles[corner++] = renderVertex;
                }
            }

            Mv3AnimationKeyFrame[] animationKeyFrames = new Mv3AnimationKeyFrame[vertFrames.Length];
            for (int i = 0; i < animationKeyFrames.Length; i++)
            {
                Mv3VertFrame frame = vertFrames[i];
                var vertices = new GameBoxVector3[normalIndices.Count];
                for (int j = 0; j < vertices.Length; j++)
                {
                    Mv3Vert vertex = frame.Vertices[positionIndices[j]];
                    vertices[j] = new GameBoxVector3(vertex.X, vertex.Y, vertex.Z);
                }

                animationKeyFrames[i] = new Mv3AnimationKeyFrame
                {
                    GameBoxTick = frame.GameBoxTick,
                    GameBoxVertices = vertices,
                };
            }

            return new Mv3Mesh
            {
                Name = name,
                GameBoxBoundsMin = gameBoxBoundsMin,
                GameBoxBoundsMax = gameBoxBoundsMax,
                Attributes = attributes,
                GameBoxTriangles = triangles,
                NormalIndices = normalIndices.ToArray(),
                NormalCount = normalCount,
                Uvs = uvs.ToArray(),
                KeyFrames = animationKeyFrames,
            };
        }

        private static Mv3TagNode ReadTagNode(IBinaryReader reader, int codepage)
        {
            string nodeName = reader.ReadString(64, codepage);
            float flipScale = reader.ReadSingle();
            int numberOfFrames = reader.ReadInt32();

            Mv3TagFrame[] tagFrames = new Mv3TagFrame[numberOfFrames];
            for (int i = 0; i < numberOfFrames; i++)
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
            uint gameBoxTick = reader.ReadUInt32();
            GameBoxVector3 gameBoxPosition = reader.ReadGameBoxVector3();

            GameBoxQuaternion gameBoxRotation = new()
            {
                X = reader.ReadSingle(),
                Y = reader.ReadSingle(),
                Z = reader.ReadSingle(),
                W = reader.ReadSingle(),
            };

            float[][] scale =
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
                Diffuse = reader.ReadColor(),
                Ambient = reader.ReadColor(),
                Specular = reader.ReadColor(),
                Emissive = reader.ReadColor(),
                SpecularPower = reader.ReadSingle()
            };

            List<string> textureNames = new ();
            for (int i = 0; i < 4; i++)
            {
                string textureName;
                int length = reader.ReadInt32();

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