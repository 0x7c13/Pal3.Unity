// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Core.DataReader.Cvd
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
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

            var animationDuration = 0f;
            var numberONodes = reader.ReadInt32();
            var rootNodes = new List<CvdGeometryNode>();
            for (var i = 0; i < numberONodes; i++)
            {
                var isGeometryNode = reader.ReadByte();
                if (isGeometryNode == 1)
                {
                    ReadGeometryNodes(reader, version, rootNodes, ref animationDuration);
                }
            }

            return new CvdFile(animationDuration, rootNodes.ToArray());
        }

        private static void ReadGeometryNodes(BinaryReader reader,
            float version,
            List<CvdGeometryNode> rootNodes,
            ref float animationDuration)
        {
            var parentNode = ReadGeometryNode(reader, version, ref animationDuration);

            var numberOfChildNodes = reader.ReadInt32();

            var children = new List<CvdGeometryNode>();
            for (var i = 0; i < numberOfChildNodes; i++)
            {
                var isGeometryNode = reader.ReadByte();
                if (isGeometryNode == 1)
                {
                    ReadGeometryNodes(reader, version, children, ref animationDuration);
                }
            }

            parentNode.Children = children.ToArray();
            rootNodes.Add(parentNode);
        }

        private static CvdGeometryNode ReadGeometryNode(BinaryReader reader,
            float version,
            ref float animationDuration)
        {
            var positionKeySize = Mathf.Max(
                Marshal.SizeOf(typeof(CvdTcbVector3Key)),
                Marshal.SizeOf(typeof(CvdBezierVector3Key)),
                Marshal.SizeOf(typeof(CvdLinearVector3Key)));

            var positionKeyInfos = ReadPositionAnimationKeyInfo(reader, positionKeySize);
            if (positionKeyInfos.Last().Time > animationDuration) animationDuration = positionKeyInfos.Last().Time;

            var rotationKeySize = Mathf.Max(
                Marshal.SizeOf(typeof(CvdTcbRotationKey)),
                Marshal.SizeOf(typeof(CvdBezierRotationKey)),
                Marshal.SizeOf(typeof(CvdLinearRotationKey)));

            var rotationKeyInfos = ReadRotationAnimationKeyInfo(reader, rotationKeySize);
            if (rotationKeyInfos.Last().Time > animationDuration) animationDuration = rotationKeyInfos.Last().Time;

            var scaleKeySize = Mathf.Max(
                Marshal.SizeOf(typeof(CvdTcbScaleKey)),
                Marshal.SizeOf(typeof(CvdBezierScaleKey)),
                Marshal.SizeOf(typeof(CvdLinearScaleKey)));

            var scaleKeyInfos = ReadScaleAnimationKeyInfo(reader, scaleKeySize);
            if (scaleKeyInfos.Last().Time > animationDuration) animationDuration = scaleKeyInfos.Last().Time;

            var scale = reader.ReadSingle();

            var mesh = ReadMesh(reader, version);
            if (mesh.AnimationTimeKeys.Last() > animationDuration) animationDuration = mesh.AnimationTimeKeys.Last();

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

            // TODO: I have not seen type other than linear so far,
            // so I just leave the log on for now.
            if (keyType != CvdAnimationKeyType.Linear)
            {
                Debug.LogError($"Key type: {keyType.ToString()}");
            }

            for (var i = 0; i < numberOfKeys; i++)
            {
                keyInfos.Add((keyType, reader.ReadBytes(size)));
            }

            return keyInfos.ToArray();
        }

        private static CvdAnimationPositionKeyFrame[] ReadPositionAnimationKeyInfo(BinaryReader reader, int size)
        {
            var frameInfos = ReadAnimationKeyInfo(reader, size);
            var positionKeyFrames = new CvdAnimationPositionKeyFrame[frameInfos.Length];

            for (var i = 0; i < frameInfos.Length; i++)
            {
                var (type, data) = frameInfos[i];

                switch (type)
                {
                    case CvdAnimationKeyType.Tcb:
                    {
                        var positionKey = Utility.ReadStruct<CvdTcbVector3Key>(data);
                        positionKeyFrames[i] = new CvdAnimationPositionKeyFrame()
                        {
                            KeyType = type,
                            Time = positionKey.TcbKey.AnimationKey.Time,
                            Position = positionKey.Value,
                        };
                        break;
                    }
                    case CvdAnimationKeyType.Bezier:
                    {
                        var positionKey = Utility.ReadStruct<CvdBezierVector3Key>(data);
                        positionKeyFrames[i] = new CvdAnimationPositionKeyFrame()
                        {
                            KeyType = type,
                            Time = positionKey.AnimationKey.Time,
                            Position = positionKey.Value,
                        };
                        break;
                    }
                    case CvdAnimationKeyType.Linear:
                    {
                        var positionKey = Utility.ReadStruct<CvdLinearVector3Key>(data);
                        positionKeyFrames[i] = new CvdAnimationPositionKeyFrame()
                        {
                            KeyType = type,
                            Time = positionKey.AnimationKey.Time,
                            Position = positionKey.Value,
                        };
                        break;
                    }
                }
            }

            return positionKeyFrames;
        }

        private static CvdAnimationRotationKeyFrame[] ReadRotationAnimationKeyInfo(BinaryReader reader, int size)
        {
            var frameInfos = ReadAnimationKeyInfo(reader, size);
            var rotationKeyFrames = new CvdAnimationRotationKeyFrame[frameInfos.Length];

            for (var i = 0; i < frameInfos.Length; i++)
            {
                var (type, data) = frameInfos[i];

                switch (type)
                {
                    case CvdAnimationKeyType.Tcb:
                    {
                        var rotationKey = Utility.ReadStruct<CvdTcbRotationKey>(data);
                        var quaternion = Quaternion.AngleAxis(rotationKey.Angle, rotationKey.Axis);
                        rotationKeyFrames[i] = new CvdAnimationRotationKeyFrame()
                        {
                            KeyType = type,
                            Time = rotationKey.TcbKey.AnimationKey.Time,
                            Rotation = new GameBoxQuaternion()
                            {
                                X = quaternion.x,
                                Y = quaternion.y,
                                Z = quaternion.z,
                                W = quaternion.w,
                            }
                        };
                        break;
                    }
                    case CvdAnimationKeyType.Bezier:
                    {
                        var rotationKey = Utility.ReadStruct<CvdBezierRotationKey>(data);
                        rotationKeyFrames[i] = new CvdAnimationRotationKeyFrame()
                        {
                            KeyType = type,
                            Time = rotationKey.AnimationKey.Time,
                            Rotation = rotationKey.Value
                        };
                        break;
                    }
                    case CvdAnimationKeyType.Linear:
                    {
                        var rotationKey = Utility.ReadStruct<CvdLinearRotationKey>(data);
                        rotationKeyFrames[i] = new CvdAnimationRotationKeyFrame()
                        {
                            KeyType = type,
                            Time = rotationKey.AnimationKey.Time,
                            Rotation = rotationKey.Value
                        };
                        break;
                    }
                }
            }

            return rotationKeyFrames;
        }

        private static CvdAnimationScaleKeyFrame[] ReadScaleAnimationKeyInfo(BinaryReader reader, int size)
        {
            var frameInfos = ReadAnimationKeyInfo(reader, size);
            var scaleKeyFrames = new CvdAnimationScaleKeyFrame[frameInfos.Length];

            for (var i = 0; i < frameInfos.Length; i++)
            {
                var (type, data) = frameInfos[i];

                switch (type)
                {
                    case CvdAnimationKeyType.Tcb:
                    {
                        var scaleKey = Utility.ReadStruct<CvdTcbScaleKey>(data);
                        scaleKeyFrames[i] = new CvdAnimationScaleKeyFrame()
                        {
                            KeyType = type,
                            Time = scaleKey.TcbKey.AnimationKey.Time,
                            Scale = scaleKey.Value,
                            Rotation = scaleKey.Rotation
                        };
                        break;
                    }
                    case CvdAnimationKeyType.Bezier:
                    {
                        var scaleKey = Utility.ReadStruct<CvdBezierScaleKey>(data);
                        scaleKeyFrames[i] = new CvdAnimationScaleKeyFrame()
                        {
                            KeyType = type,
                            Time = scaleKey.AnimationKey.Time,
                            Scale = scaleKey.Value,
                            Rotation = scaleKey.Rotation
                        };
                        break;
                    }
                    case CvdAnimationKeyType.Linear:
                    {
                        var scaleKey = Utility.ReadStruct<CvdLinearScaleKey>(data);
                        scaleKeyFrames[i] = new CvdAnimationScaleKeyFrame()
                        {
                            KeyType = type,
                            Time = scaleKey.AnimationKey.Time,
                            Scale = scaleKey.Value,
                            Rotation = scaleKey.Rotation
                        };
                        break;
                    }
                }
            }

            return scaleKeyFrames;
        }

        private static CvdMesh ReadMesh(BinaryReader reader, float version)
        {
            var numberOfFrames = reader.ReadInt32();
            var numberOfVertices = reader.ReadInt32();

            //var vertexType = GameBoxVertexType.XYZ | GameBoxVertexType.Normal | GameBoxVertexType.UV0;
            //var vertexSize = GameBoxVertex.GetSize((uint)vertexType);

            var frameVertices = new List<CvdVertex[]>();
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
                frameVertices.Add(vertices.ToArray());
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
                meshSections.Add(ReadMeshSection(reader, version, frameVertices));
            }

            return new CvdMesh()
            {
                AnimationTimeKeys = animationTimeKeys,
                MeshSections = meshSections.ToArray()
            };
        }

        private static CvdMeshSection ReadMeshSection(BinaryReader reader,
            float version,
            List<CvdVertex[]> allFrameVertices)
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

            var numberOfIndices = reader.ReadInt32();

            var indices = new List<(short x, short y, short z)>();
            for (var i = 0; i < numberOfIndices; i++)
            {
                var x = reader.ReadInt16();
                var y = reader.ReadInt16();
                var z = reader.ReadInt16();
                indices.Add(new (x, y, z));
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

            List<CvdVertex[]> frameVertices = new List<CvdVertex[]>();
            List<int> triangles;
            List<int> indexBuffer;

            (triangles, indexBuffer) = CalculateTriangles(indices);

            for (var i = 0; i < allFrameVertices.Count; i++)
            {
                var verts = new List<CvdVertex>();
                var allVertices = allFrameVertices[i];

                for (var j = 0; j < indexBuffer.Count; j++)
                {
                    verts.Add(allVertices[indexBuffer[j]]);
                }

                frameVertices.Add(verts.ToArray());
            }

            return new CvdMeshSection()
            {
                BlendFlag = blendFlag,
                Material = material,
                TextureName = textureName,
                FrameVertices = frameVertices.ToArray(),
                Triangles = triangles.ToArray(),
                AnimationTimeKeys = animationTimeKeys,
                AnimationMaterials = animationMaterials.ToArray()
            };
        }

        private static (List<int> triangles, List<int> indexBuffer) CalculateTriangles(
            List<(short x, short y, short z)> allIndices)
        {
            var indexBuffer = new List<int>();
            var triangles = new List<int>();
            var indexMap = new Dictionary<int, int>();

            for (var j = 0; j < allIndices.Count; j++)
            {
                var indices = new[]
                {
                    allIndices[j].x,
                    allIndices[j].y,
                    allIndices[j].z
                };

                for (var k = 0; k < 3; k++)
                {
                    if (indexMap.ContainsKey(indices[k]))
                    {
                        triangles.Add(indexMap[indices[k]]);
                    }
                    else
                    {
                        var index = indexBuffer.Count;
                        indexBuffer.Add(indices[k]);
                        indexMap[indices[k]] = index;
                        triangles.Add(index);
                    }
                }
            }

            return (triangles, indexBuffer);
        }
    }
}