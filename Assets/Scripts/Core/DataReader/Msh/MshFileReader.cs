﻿// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

#define USE_UNSAFE_BINARY_READER

namespace Core.DataReader.Msh
{
    using System.IO;
    using Extensions;
    using GameBox;
    using UnityEngine;
    using Utils;

    public sealed class MshFileReader : IFileReader<MshFile>
    {
        private readonly int _codepage;

        public MshFileReader(int codepage)
        {
            _codepage = codepage;
        }

        public MshFile Read(byte[] data)
        {
            #if USE_UNSAFE_BINARY_READER
            using var reader = new UnsafeBinaryReader(data);
            #else
            using var stream = new MemoryStream(data);
            using var reader = new BinaryReader(stream);
            #endif

            var header = reader.ReadChars(4);
            var headerStr = new string(header[..^1]);

            if (headerStr != "msh")
            {
                throw new InvalidDataException("Invalid MSH(.msh) file: header != msh");
            }

            var version = reader.ReadInt32();
            if (version != 100)
            {
                throw new InvalidDataException("Invalid MSH(.msh) file: version != 100");
            }

            int skeletonDataOffset = reader.ReadInt32();
            int meshDataOffset = reader.ReadInt32();

            BoneNode rootBoneNode = new()
            {
                Type = BoneNodeType.Normal,
                Id = 0,
                Name = "root",
                Parent = null,
                ParentId = 0,
            };

            var numberOfChildBones = reader.ReadInt32();
            var childBoneNodes = new BoneNode[numberOfChildBones];
            for (var i = 0; i < numberOfChildBones; i++)
            {
                childBoneNodes[i] = ReadBoneNode(reader, rootBoneNode, _codepage);
            }

            rootBoneNode.Children = childBoneNodes;

            var numberOfSubMeshes = reader.ReadInt32();
            var subMeshes = new MshMesh[numberOfSubMeshes];
            for (var i = 0; i < numberOfSubMeshes; i++)
            {
                subMeshes[i] = ReadSubMesh(reader, _codepage);
            }

            return new MshFile(rootBoneNode, subMeshes);
        }

        #if USE_UNSAFE_BINARY_READER
        private static BoneNode ReadBoneNode(UnsafeBinaryReader reader, BoneNode parent, int codepage)
        #else
        private static BoneNode ReadBoneNode(BinaryReader reader, BoneNode parent, int codepage)
        #endif
        {
            var nodeType = (BoneNodeType)reader.ReadInt32();
            var nameLength = reader.ReadInt32();
            var name = reader.ReadString(nameLength, codepage);

            Vector3 translation = GameBoxInterpreter.ToUnityPosition(reader.ReadVector3());
            Quaternion rotation = GameBoxInterpreter.MshQuaternionToUnityQuaternion(new GameBoxQuaternion()
            {
                X = reader.ReadSingle(),
                Y = reader.ReadSingle(),
                Z = reader.ReadSingle(),
                W = reader.ReadSingle(),
            });

            Matrix4x4 scaleMatrix = GameBoxInterpreter.ToUnityMatrix4x4(new GameBoxMatrix4X4()
            {
                Xx = reader.ReadSingle(), Xy = reader.ReadSingle(), Xz = reader.ReadSingle(), Xw = 0,
                Yx = reader.ReadSingle(), Yy = reader.ReadSingle(), Yz = reader.ReadSingle(), Yw = 0,
                Zx = reader.ReadSingle(), Zy = reader.ReadSingle(), Zz = reader.ReadSingle(), Zw = 0,
                Tx = 0, Ty = 0, Tz = 0, Tw = 1,
            });

            float flipScaleSign = reader.ReadSingle();
            Matrix4x4 localTransformMatrix = GameBoxInterpreter.ToUnityMatrix4x4(new GameBoxMatrix4X4()
            {
                Xx = reader.ReadSingle(), Xy = reader.ReadSingle(), Xz = reader.ReadSingle(), Xw = reader.ReadSingle(),
                Yx = reader.ReadSingle(), Yy = reader.ReadSingle(), Yz = reader.ReadSingle(), Yw = reader.ReadSingle(),
                Zx = reader.ReadSingle(), Zy = reader.ReadSingle(), Zz = reader.ReadSingle(), Zw = reader.ReadSingle(),
                Tx = reader.ReadSingle(), Ty = reader.ReadSingle(), Tz = reader.ReadSingle(), Tw = reader.ReadSingle()
            });

            var boneID = reader.ReadInt32();
            var parentBoneID = reader.ReadInt32();
            var numberOfChildBones = reader.ReadInt32();

            BoneNode boneNode = new()
            {
                Type = nodeType,
                Id = boneID,
                Name = name,
                Parent = parent,
                ParentId = parentBoneID,
                Translation = translation,
                Rotation = rotation,
                Scale = scaleMatrix,
                FlipScaleSign = flipScaleSign,
                LocalTransform = localTransformMatrix,
            };

            var childBoneNodes = new BoneNode[numberOfChildBones];
            for (var i = 0; i < numberOfChildBones; i++)
            {
                childBoneNodes[i] = ReadBoneNode(reader, boneNode, codepage);
            }

            boneNode.Children = childBoneNodes;

            return boneNode;
        }

        #if USE_UNSAFE_BINARY_READER
        private static MshMesh ReadSubMesh(UnsafeBinaryReader reader, int codepage)
        #else
        private static MshMesh ReadSubMesh(BinaryReader reader, int codepage)
        #endif
        {
            var materialId = reader.ReadInt32();
            var numberOfVertices = reader.ReadInt32();
            var numberOfFaces = reader.ReadInt32();
            var numberOfSprings = reader.ReadInt32();
            var numberOfLods = reader.ReadInt32();
            var layerFlag = (MshLayerFlag)reader.ReadInt32();

            var vertices = new Vector3[numberOfVertices];

            if (layerFlag.HasFlag(MshLayerFlag.Position))
            {
                for (var i = 0; i < numberOfVertices; i++)
                {
                    vertices[i] = GameBoxInterpreter.ToUnityPosition(reader.ReadVector3());
                }
            }

            var phyVertices = new PhyVertex[numberOfVertices];
            if (layerFlag.HasFlag(MshLayerFlag.Bone))
            {
                for (var i = 0; i < numberOfVertices; i++)
                {
                    var numberOfBones = reader.ReadByte();
                    var boneIds = new byte[numberOfBones];
                    var weights = new byte[numberOfBones];
                    for (var j = 0; j < numberOfBones; j++)
                    {
                        boneIds[j] = reader.ReadByte();
                        weights[j] = reader.ReadByte();
                    }

                    phyVertices[i] = new PhyVertex
                    {
                        Position = vertices[i],
                        BoneIds = boneIds,
                        Weights = weights,
                    };
                }
            }

            var phyFaces = new PhyFace[numberOfFaces];
            for (var i = 0; i < numberOfFaces; i++)
            {
                phyFaces[i] = new PhyFace(3, 4, 3)
                {
                    Indices =
                    {
                        [0] = reader.ReadInt32(),
                        [1] = reader.ReadInt32(),
                        [2] = reader.ReadInt32()
                    },
                    Normals =
                    {
                        [0] = reader.ReadInt16(),
                        [1] = reader.ReadInt16(),
                        [2] = reader.ReadInt16(),
                        [3] = reader.ReadInt16()
                    },
                    U =
                    {
                        [0, 0] = reader.ReadSingle(),
                        [0, 1] = reader.ReadSingle(),
                        [1, 0] = reader.ReadSingle(),
                        [1, 1] = reader.ReadSingle(),
                        [2, 0] = reader.ReadSingle(),
                        [2, 1] = reader.ReadSingle()
                    },
                    V =
                    {
                        [0, 0] = reader.ReadSingle(),
                        [0, 1] = reader.ReadSingle(),
                        [1, 0] = reader.ReadSingle(),
                        [1, 1] = reader.ReadSingle(),
                        [2, 0] = reader.ReadSingle(),
                        [2, 1] = reader.ReadSingle()
                    }
                };

                for (var j = 0; j < 3; j++)
                {
                    phyFaces[i].U[j, 0] = ((int)(phyFaces[i].U[j, 0] * 255)) / 255f;
                    phyFaces[i].V[j, 0] = ((int)(phyFaces[i].V[j, 0] * 255)) / 255f;
                }
            }

            return new MshMesh()
            {
                MaterialId = materialId,
                Vertices = phyVertices,
                Faces = phyFaces,
            };
        }
    }
}