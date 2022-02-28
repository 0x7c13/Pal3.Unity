// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE.txt in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Core.DataReader.Cvd
{
    using System.Collections.Generic;
    using System.IO;
    using System.Runtime.InteropServices;
    using Extensions;
    using GameBox;
    using UnityEngine;
    using Utils;

    public static class CvdFileReader
    {
        public static CvdFile Read(Stream stream)
        {
            using var reader = new BinaryReader(stream);

            var header = reader.ReadChars(4);
            var headerStr = new string(header);

            var version = headerStr switch
            {
                "cvdf" => 0.4f,
                "cvds" => 0.5f,
                _ => throw new InvalidDataException($"Invalid CVD(.cvd) file: header != cvdf or cvds")
            };

            var numberONodes = reader.ReadInt32();
            var rootNodes = new List<CvdGeometryNode>();
            for (var i = 0; i < numberONodes; i++)
            {
                var isGeometryNode = reader.ReadByte();
                if (isGeometryNode == 1)
                {
                    ReadGeometryNodes(reader, version, rootNodes);
                }
            }

            return new CvdFile(rootNodes.ToArray());
        }

        private static void ReadGeometryNodes(BinaryReader reader,
            float version,
            List<CvdGeometryNode> rootNodes)
        {
            var parentNode = ReadGeometryNode(reader, version);

            var numberOfChildNodes = reader.ReadInt32();

            var children = new List<CvdGeometryNode>();
            for (var i = 0; i < numberOfChildNodes; i++)
            {
                var isGeometryNode = reader.ReadByte();
                if (isGeometryNode == 1)
                {
                    ReadGeometryNodes(reader, version, children);
                }
            }

            parentNode.Children = children.ToArray();
            rootNodes.Add(parentNode);
        }

        private static CvdGeometryNode ReadGeometryNode(BinaryReader reader,
            float version)
        {
            var positionKeySize = Mathf.Max(
                Marshal.SizeOf(typeof(CvdTcbVector3Key)),
                Marshal.SizeOf(typeof(CvdBezierVector3Key)),
                Marshal.SizeOf(typeof(CvdLinearVector3Key)));

            var positionKeyInfos = ReadAnimationKeyInfo(reader, positionKeySize);

            var rotationKeySize = Mathf.Max(
                Marshal.SizeOf(typeof(CvdTcbRotationKey)),
                Marshal.SizeOf(typeof(CvdBezierRotationKey)),
                Marshal.SizeOf(typeof(CvdLinearRotationKey)));

            var rotationKeyInfos = ReadAnimationKeyInfo(reader, rotationKeySize);

            var scaleKeySize = Mathf.Max(
                Marshal.SizeOf(typeof(CvdTcbScaleKey)),
                Marshal.SizeOf(typeof(CvdBezierScaleKey)),
                Marshal.SizeOf(typeof(CvdLinearScaleKey)));

            var scaleKeyInfos = ReadAnimationKeyInfo(reader, scaleKeySize);

            var scale = reader.ReadSingle();

            var mesh = ReadMesh(reader, version);

            var transformMatrix = new GameBoxMatrix4X4()
            {
                Xx = reader.ReadSingle(), Xy = reader.ReadSingle(), Xz = reader.ReadSingle(),
                Xw = reader.ReadSingle(),
                Yx = reader.ReadSingle(), Yy = reader.ReadSingle(), Yz = reader.ReadSingle(),
                Yw = reader.ReadSingle(),
                Zx = reader.ReadSingle(), Zy = reader.ReadSingle(), Zz = reader.ReadSingle(),
                Zw = reader.ReadSingle(),
                Tx = reader.ReadSingle(), Ty = reader.ReadSingle(), Tz = reader.ReadSingle(),
                Tw = reader.ReadSingle()
            };
            transformMatrix.Tw = 1f;

            return new CvdGeometryNode()
            {
                PositionKeyInfos = positionKeyInfos,
                RotationKeyInfos = rotationKeyInfos,
                ScaleKeyInfos = scaleKeyInfos,
                Scale = scale,
                TransformMatrix = transformMatrix,
                Mesh = mesh
            };
        }

        private static (CvdAnimationKeyType, byte[])[] ReadAnimationKeyInfo(BinaryReader reader, int size)
        {
            var numberOfKeys = reader.ReadInt32();
            var keyInfos = new List<(CvdAnimationKeyType, byte[])>();

            var keyType = (CvdAnimationKeyType)reader.ReadByte();

            for (var i = 0; i < numberOfKeys; i++)
            {
                keyInfos.Add((keyType, reader.ReadBytes(size)));
            }

            return keyInfos.ToArray();
        }

        private static CvdMesh ReadMesh(BinaryReader reader, float version)
        {
            var numberOfFrames = reader.ReadInt32();
            var numberOfVertices = reader.ReadInt32();

            //var vertexType = GameBoxVertexType.XYZ | GameBoxVertexType.Normal | GameBoxVertexType.UV0;
            //var vertexSize = GameBoxVertex.GetSize((uint)vertexType);

            var frames = new List<CvdVertex[]>();
            for (var i = 0; i < numberOfFrames; i++)
            {
                var vertices = new List<CvdVertex>();
                for (var j = 0; j < numberOfVertices; j++)
                {
                    var uv = reader.ReadVector2();
                    var normal = reader.ReadVector3();
                    var position = reader.ReadVector3();

                    vertices.Add(new CvdVertex()
                    {
                        Normal = normal,
                        Position = position,
                        Uv = uv
                    });
                }
                frames.Add(vertices.ToArray());
            }

            var animationTimeKeys = reader.ReadSingleArray(numberOfFrames);
            for (var i = 0; i < numberOfFrames; i++)
            {
                animationTimeKeys[i] -= animationTimeKeys[0];
            }

            var numberOfMeshes = reader.ReadInt32();
            var meshSections = new List<CvdMeshSection>();
            for (var i = 0; i < numberOfMeshes; i++)
            {
                meshSections.Add(ReadMeshSection(reader, version));
            }

            return new CvdMesh()
            {
                Frames = frames.ToArray(),
                AnimationTimeKeys = animationTimeKeys,
                MeshSections = meshSections.ToArray()
            };
        }

        private static CvdMeshSection ReadMeshSection(BinaryReader reader, float version)
        {
            var blendFlag = reader.ReadByte();

            var material = new GameBoxMaterial()
            {
                Diffuse = Utility.ToColor32(reader.ReadBytes(4)),
                Ambient = Utility.ToColor32(reader.ReadBytes(4)),
                Specular = Utility.ToColor32(reader.ReadBytes(4)),
                Emissive = Utility.ToColor32(reader.ReadBytes(4)),
                Power = reader.ReadSingle()
            };

            var textureName = reader.ReadGbkString(64);

            var numberOfTriangles = reader.ReadInt32();

            var triangles = new List<(short x, short y, short z)>();
            for (var i = 0; i < numberOfTriangles; i++)
            {
                var x = reader.ReadInt16();
                var y = reader.ReadInt16();
                var z = reader.ReadInt16();
                triangles.Add(new (x, y, z));
            }

            var animationTimeKeys = new float[] {};
            var animationMaterials = new List<GameBoxMaterial>();
            if (version >= 0.5)
            {
                var numberOfFrames = reader.ReadInt32();

                animationTimeKeys = reader.ReadSingleArray(numberOfFrames);
                for (var i = 0; i < numberOfFrames; i++)
                {
                    animationTimeKeys[i] -= animationTimeKeys[0];
                }

                for (var i = 0; i < numberOfFrames; i++)
                {
                    animationMaterials.Add(new GameBoxMaterial()
                    {
                        Diffuse = Utility.ToColor(reader.ReadSingleArray(4)),
                        Ambient = Utility.ToColor(reader.ReadSingleArray(4)),
                        Specular = Utility.ToColor(reader.ReadSingleArray(4)),
                        Emissive = Utility.ToColor(reader.ReadSingleArray(4)),
                        Power = reader.ReadSingle()
                    });
                }
            }

            return new CvdMeshSection()
            {
                BlendFlag = blendFlag,
                Material = material,
                TextureName = textureName,
                Triangles = triangles.ToArray(),
                AnimationTimeKeys = animationTimeKeys,
                AnimationMaterials = animationMaterials.ToArray()
            };
        }
    }
}