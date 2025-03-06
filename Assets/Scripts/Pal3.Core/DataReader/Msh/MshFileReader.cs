// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2025, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Core.DataReader.Msh
{
    using System.IO;
    using Primitives;

    public sealed class MshFileReader : IFileReader<MshFile>
    {
        public MshFile Read(IBinaryReader reader, int codepage)
        {
            char[] header = reader.ReadChars(4);
            string headerStr = new (header[..^1]);

            if (headerStr != "msh")
            {
                throw new InvalidDataException("Invalid MSH(.msh) file: header != msh");
            }

            int version = reader.ReadInt32();
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

            int numberOfChildBones = reader.ReadInt32();
            BoneNode[] childBoneNodes = new BoneNode[numberOfChildBones];
            for (int i = 0; i < numberOfChildBones; i++)
            {
                childBoneNodes[i] = ReadBoneNode(reader, rootBoneNode, codepage);
            }

            rootBoneNode.Children = childBoneNodes;

            int numberOfSubMeshes = reader.ReadInt32();
            MshMesh[] subMeshes = new MshMesh[numberOfSubMeshes];
            for (int i = 0; i < numberOfSubMeshes; i++)
            {
                subMeshes[i] = ReadSubMesh(reader);
            }

            return new MshFile(rootBoneNode, subMeshes);
        }

        private static BoneNode ReadBoneNode(IBinaryReader reader, BoneNode parent, int codepage)
        {
            BoneNodeType nodeType = (BoneNodeType)reader.ReadInt32();
            int nameLength = reader.ReadInt32();
            string name = reader.ReadString(nameLength, codepage);

            GameBoxVector3 gameBoxTranslation = reader.ReadGameBoxVector3();
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

            int boneID = reader.ReadInt32();
            int parentBoneID = reader.ReadInt32();
            int numberOfChildBones = reader.ReadInt32();

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

            BoneNode[] childBoneNodes = new BoneNode[numberOfChildBones];
            for (int i = 0; i < numberOfChildBones; i++)
            {
                childBoneNodes[i] = ReadBoneNode(reader, boneNode, codepage);
            }

            boneNode.Children = childBoneNodes;

            return boneNode;
        }

        private static MshMesh ReadSubMesh(IBinaryReader reader)
        {
            int materialId = reader.ReadInt32();
            int numberOfVertices = reader.ReadInt32();
            int numberOfFaces = reader.ReadInt32();
            int numberOfSprings = reader.ReadInt32();
            int numberOfLods = reader.ReadInt32();
            MshLayerFlag layerFlag = (MshLayerFlag)reader.ReadInt32();

            GameBoxVector3[] gameBoxVertices = null;
            if (layerFlag.HasFlag(MshLayerFlag.Position))
            {
                gameBoxVertices = reader.ReadGameBoxVector3s(numberOfVertices);
            }

            PhyVertex[] phyVertices = new PhyVertex[numberOfVertices];
            if (layerFlag.HasFlag(MshLayerFlag.Bone))
            {
                for (int i = 0; i < numberOfVertices; i++)
                {
                    byte numberOfBones = reader.ReadByte();
                    byte[] boneIds = new byte[numberOfBones];
                    byte[] weights = new byte[numberOfBones];
                    for (int j = 0; j < numberOfBones; j++)
                    {
                        boneIds[j] = reader.ReadByte();
                        weights[j] = reader.ReadByte();
                    }

                    phyVertices[i].GameBoxPosition = gameBoxVertices != null ? gameBoxVertices[i] : new GameBoxVector3();
                    phyVertices[i].BoneIds = boneIds;
                    phyVertices[i].Weights = weights;
                }
            }

            PhyFace[] phyFaces = new PhyFace[numberOfFaces];
            for (int i = 0; i < numberOfFaces; i++)
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

                for (int j = 0; j < 3; j++)
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