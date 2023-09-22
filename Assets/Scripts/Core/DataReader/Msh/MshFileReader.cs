// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Core.DataReader.Msh
{
    using System.IO;
    using Primitives;

    public sealed class MshFileReader : IFileReader<MshFile>
    {
        public MshFile Read(IBinaryReader reader, int codepage)
        {
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
                childBoneNodes[i] = ReadBoneNode(reader, rootBoneNode, codepage);
            }

            rootBoneNode.Children = childBoneNodes;

            var numberOfSubMeshes = reader.ReadInt32();
            var subMeshes = new MshMesh[numberOfSubMeshes];
            for (var i = 0; i < numberOfSubMeshes; i++)
            {
                subMeshes[i] = ReadSubMesh(reader);
            }

            return new MshFile(rootBoneNode, subMeshes);
        }

        private static BoneNode ReadBoneNode(IBinaryReader reader, BoneNode parent, int codepage)
        {
            var nodeType = (BoneNodeType)reader.ReadInt32();
            var nameLength = reader.ReadInt32();
            var name = reader.ReadString(nameLength, codepage);

            GameBoxVector3 gameBoxTranslation = reader.ReadVector3();
            GameBoxQuaternion gameBoxRotation = new GameBoxQuaternion()
            {
                X = reader.ReadSingle(),
                Y = reader.ReadSingle(),
                Z = reader.ReadSingle(),
                W = reader.ReadSingle(),
            };

            GameBoxMatrix4x4 gameBoxScaleMatrix = new GameBoxMatrix4x4()
            {
                Xx = reader.ReadSingle(), Xy = reader.ReadSingle(), Xz = reader.ReadSingle(), Xw = 0,
                Yx = reader.ReadSingle(), Yy = reader.ReadSingle(), Yz = reader.ReadSingle(), Yw = 0,
                Zx = reader.ReadSingle(), Zy = reader.ReadSingle(), Zz = reader.ReadSingle(), Zw = 0,
                Tx = 0, Ty = 0, Tz = 0, Tw = 1,
            };

            float flipScaleSign = reader.ReadSingle();
            GameBoxMatrix4x4 gameBoxLocalTransformMatrix = new GameBoxMatrix4x4()
            {
                Xx = reader.ReadSingle(), Xy = reader.ReadSingle(), Xz = reader.ReadSingle(), Xw = reader.ReadSingle(),
                Yx = reader.ReadSingle(), Yy = reader.ReadSingle(), Yz = reader.ReadSingle(), Yw = reader.ReadSingle(),
                Zx = reader.ReadSingle(), Zy = reader.ReadSingle(), Zz = reader.ReadSingle(), Zw = reader.ReadSingle(),
                Tx = reader.ReadSingle(), Ty = reader.ReadSingle(), Tz = reader.ReadSingle(), Tw = reader.ReadSingle()
            };

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
                GameBoxTranslation = gameBoxTranslation,
                GameBoxRotation = gameBoxRotation,
                GameBoxScale = gameBoxScaleMatrix,
                FlipScaleSign = flipScaleSign,
                GameBoxLocalTransform = gameBoxLocalTransformMatrix,
            };

            var childBoneNodes = new BoneNode[numberOfChildBones];
            for (var i = 0; i < numberOfChildBones; i++)
            {
                childBoneNodes[i] = ReadBoneNode(reader, boneNode, codepage);
            }

            boneNode.Children = childBoneNodes;

            return boneNode;
        }

        private static MshMesh ReadSubMesh(IBinaryReader reader)
        {
            var materialId = reader.ReadInt32();
            var numberOfVertices = reader.ReadInt32();
            var numberOfFaces = reader.ReadInt32();
            var numberOfSprings = reader.ReadInt32();
            var numberOfLods = reader.ReadInt32();
            var layerFlag = (MshLayerFlag)reader.ReadInt32();

            var gameBoxVertices = new GameBoxVector3[numberOfVertices];

            if (layerFlag.HasFlag(MshLayerFlag.Position))
            {
                for (var i = 0; i < numberOfVertices; i++)
                {
                    gameBoxVertices[i] = reader.ReadVector3();
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
                        GameBoxPosition = gameBoxVertices[i],
                        BoneIds = boneIds,
                        Weights = weights,
                    };
                }
            }

            var phyFaces = new PhyFace[numberOfFaces];
            for (var i = 0; i < numberOfFaces; i++)
            {
                phyFaces[i] = new PhyFace
                {
                    Indices = new []
                    {
                        reader.ReadInt32(),
                        reader.ReadInt32(),
                        reader.ReadInt32()
                    },
                    Normals = new []
                    {
                        reader.ReadUInt16(),
                        reader.ReadUInt16(),
                        reader.ReadUInt16(),
                        reader.ReadUInt16()
                    },
                    U = new [,]
                    {
                        { reader.ReadSingle(), reader.ReadSingle() },
                        { reader.ReadSingle(), reader.ReadSingle() },
                        { reader.ReadSingle(), reader.ReadSingle() }
                    },
                    V = new [,]
                    {
                        { reader.ReadSingle(), reader.ReadSingle() },
                        { reader.ReadSingle(), reader.ReadSingle() },
                        { reader.ReadSingle(), reader.ReadSingle() }
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