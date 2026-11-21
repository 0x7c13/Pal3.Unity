// ------------------------------------------------------------------------
//  Copyright (c) 2021-2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Core.DataReader.Mv3
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using Primitives;
    using Utilities;

    public sealed class Mv3FileReader : IFileReader<Mv3File>
    {
        private readonly bool _includeNativeNormals;

        public Mv3FileReader() : this(false)
        {
        }

        public Mv3FileReader(bool includeNativeNormals)
        {
            _includeNativeNormals = includeNativeNormals;
        }

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

            ValidateCount(reader, numberOfAnimationEvents, 20, "animation event");
            Mv3AnimationEvent[] animationEvents = new Mv3AnimationEvent[numberOfAnimationEvents];
            for (int i = 0; i < numberOfAnimationEvents; i++)
            {
                animationEvents[i] = ReadAnimationEvent(reader, codepage);
            }

            ValidateCount(reader, numberOfTagNodes, 72, "tag node");
            Mv3TagNode[] tagNodes = new Mv3TagNode[numberOfTagNodes];
            for (int i = 0; i < numberOfTagNodes; i++)
            {
                tagNodes[i] = ReadTagNode(reader, codepage);
            }

            ValidateCount(reader, numberOfMaterials, 84, "material");
            GameBoxMaterial[] materials = new GameBoxMaterial[numberOfMaterials];
            for (int i = 0; i < numberOfMaterials; i++)
            {
                materials[i] = ReadMaterial(reader, codepage);
            }

            ValidateCount(reader, numberOfMeshes, 104, "mesh");
            var meshes = new List<Mv3Mesh>();
            for (int i = 0; i < numberOfMeshes; i++)
            {
                meshes.AddRange(ReadMesh(reader, codepage));
            }

            return new Mv3File(duration,
                animationEvents,
                tagNodes,
                meshes.ToArray(),
                materials);
        }

        private IEnumerable<Mv3Mesh> ReadMesh(IBinaryReader reader, int codepage)
        {
            string name = reader.ReadString(64, codepage);
            int numberOfVertices = reader.ReadInt32();
            ValidateCount(reader, numberOfVertices, 1, "vertex");

            GameBoxVector3 gameBoxBoundsMin = reader.ReadGameBoxVector3();
            GameBoxVector3 gameBoxBoundsMax = reader.ReadGameBoxVector3();

            int numberOfFrames = reader.ReadInt32();
            ValidateCount(reader, numberOfFrames, 4 + 8L * numberOfVertices, "vertex frame");
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
            ValidateCount(reader, numberOfTexCoords, 8, "texture coordinate");
            GameBoxVector2[] texCoords = numberOfTexCoords == 0 ?
                new GameBoxVector2[] { new(0f, 0f) } :
                reader.ReadGameBoxVector2s(numberOfTexCoords);

            int numberOfAttributes = reader.ReadInt32();
            ValidateCount(reader, numberOfAttributes, 12, "mesh attribute");
            if (numberOfFrames == 0 && numberOfAttributes > 0)
                throw new InvalidDataException("Invalid MV3 mesh: attributes require at least one vertex frame.");
            Mv3Attribute[] attributes = new Mv3Attribute[numberOfAttributes];
            for (int i = 0; i < numberOfAttributes; i++)
            {
                int materialId = reader.ReadInt32();
                int numberOfTriangles = reader.ReadInt32();

                ValidateCount(reader, numberOfTriangles, 12, "triangle");
                Mv3IndexBuffer[] triangles = new Mv3IndexBuffer[numberOfTriangles];
                for (int j = 0; j < numberOfTriangles; j++)
                {
                    ushort[] positions = ReadTriangleIndices(reader, numberOfVertices, "vertex");
                    ushort[] coordinates = ReadTriangleIndices(reader, texCoords.Length, "texture coordinate");
                    triangles[j] = new Mv3IndexBuffer { TriangleIndex = positions, TexCoordIndex = coordinates };
                }

                int numberOfCommands = reader.ReadInt32();
                ValidateCount(reader, numberOfCommands, 4, "command");
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

            ushort[][] nativeNormals = _includeNativeNormals ? Mv3NativeNormals.Calculate(frames, attributes) : null;
            for (int i = 0; i < attributes.Length; i++)
            {
                yield return GetMv3Mesh(attributes.Length == 1 ? name : name + " / " + i, gameBoxBoundsMin,
                    gameBoxBoundsMax, new[] { attributes[i] }, frames, texCoords, nativeNormals);
            }
        }

        private static void ValidateCount(IBinaryReader reader, int count, long recordBytes, string field)
        {
            if (count < 0 || count > (reader.Length - reader.Position) / recordBytes)
                throw new InvalidDataException($"Invalid MV3 {field} count {count} for the remaining file data.");
        }

        private static ushort[] ReadTriangleIndices(IBinaryReader reader, int count, string field)
        {
            var indices = new ushort[3];
            for (int corner = 0; corner < indices.Length; corner++)
            {
                ushort index = reader.ReadUInt16();
                if (index >= count)
                    throw new InvalidDataException($"Invalid MV3 triangle {field} index {index} with {count} entries.");
                indices[corner] = index;
            }
            return indices;
        }

        private static Mv3Mesh GetMv3Mesh(string name,
            GameBoxVector3 gameBoxBoundsMin,
            GameBoxVector3 gameBoxBoundsMax,
            Mv3Attribute[] attributes,
            Mv3VertFrame[] vertFrames,
            GameBoxVector2[] texCoords,
            ushort[][] nativeNormals)
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

            int[] vertexRepresentatives = GetVertexRepresentatives(vertFrames, positionCount);
            var (normalCorners, normalCount) = CoreUtility.GetNormalIndices(sourceCorners, positionCount, vertexRepresentatives);
            var renderVertexByCorner = new Dictionary<(int normalIndex, ushort uv, ushort source), int>();
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
                    var key = (normalIndex, uv: face.TexCoordIndex[j], source: face.TriangleIndex[j]);
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
                ushort[] packedNormals = nativeNormals == null ? null : new ushort[vertices.Length];
                for (int j = 0; j < vertices.Length; j++)
                {
                    Mv3Vert vertex = frame.Vertices[positionIndices[j]];
                    vertices[j] = new GameBoxVector3(vertex.X, vertex.Y, vertex.Z);
                    if (packedNormals != null)
                        packedNormals[j] = nativeNormals[i][positionIndices[j]];
                }

                animationKeyFrames[i] = new Mv3AnimationKeyFrame
                {
                    GameBoxTick = frame.GameBoxTick,
                    GameBoxVertices = vertices,
                    NativeNormals = packedNormals,
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

        private static int[] GetVertexRepresentatives(Mv3VertFrame[] frames, int vertexCount)
        {
            var verticesByTrajectory = new Dictionary<int, int>(vertexCount, new VertexTrajectoryComparer(frames));
            var vertexRepresentatives = new int[vertexCount];
            for (int vertex = 0; vertex < vertexCount; vertex++)
            {
                vertexRepresentatives[vertex] = verticesByTrajectory.TryAdd(vertex, vertex)
                    ? vertex
                    : verticesByTrajectory[vertex];
            }
            return verticesByTrajectory.Count < vertexCount ? vertexRepresentatives : null;
        }

        private sealed class VertexTrajectoryComparer : IEqualityComparer<int>
        {
            private readonly Mv3VertFrame[] _frames;

            public VertexTrajectoryComparer(Mv3VertFrame[] frames) => _frames = frames;

            public bool Equals(int a, int b)
            {
                foreach (Mv3VertFrame frame in _frames)
                {
                    Mv3Vert first = frame.Vertices[a];
                    Mv3Vert second = frame.Vertices[b];
                    if (first.X != second.X || first.Y != second.Y || first.Z != second.Z)
                        return false;
                }
                return true;
            }

            public int GetHashCode(int index)
            {
                var hash = new HashCode();
                foreach (Mv3VertFrame frame in _frames)
                {
                    Mv3Vert vertex = frame.Vertices[index];
                    hash.Add(vertex.X);
                    hash.Add(vertex.Y);
                    hash.Add(vertex.Z);
                }
                return hash.ToHashCode();
            }
        }

        private static Mv3TagNode ReadTagNode(IBinaryReader reader, int codepage)
        {
            string nodeName = reader.ReadString(64, codepage);
            float flipScale = reader.ReadSingle();
            int numberOfFrames = reader.ReadInt32();

            ValidateCount(reader, numberOfFrames, 56, "tag frame");
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
            GameBoxMaterial material = new()
            {
                Diffuse = reader.ReadColor(),
                Ambient = reader.ReadColor(),
                Specular = reader.ReadColor(),
                Emissive = reader.ReadColor(),
                SpecularPower = reader.ReadSingle()
            };

            List<string> textureNames = new();
            for (int i = 0; i < 4; i++)
            {
                string textureName;
                int length = reader.ReadInt32();
                ValidateCount(reader, length, 1, "texture name byte");

                if (length == 0)
                {
                    if (i > 0)
                        continue;
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
