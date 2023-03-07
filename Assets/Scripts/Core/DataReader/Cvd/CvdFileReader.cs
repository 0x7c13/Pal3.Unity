// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

#define USE_UNSAFE_BINARY_READER

namespace Core.DataReader.Cvd
{
    using System;
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
        public static CvdFile Read(byte[] data, int codepage)
        {
            #if USE_UNSAFE_BINARY_READER
            using var reader = new UnsafeBinaryReader(data);
            #else
            using var stream = new MemoryStream(data);
            using var reader = new BinaryReader(stream);
            #endif

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
                ReadGeometryNodes(reader, version, rootNodes, ref animationDuration, codepage);
            }

            return new CvdFile(animationDuration, rootNodes.ToArray());
        }

        #if USE_UNSAFE_BINARY_READER
        private static void ReadGeometryNodes(UnsafeBinaryReader reader,
            float version,
            List<CvdGeometryNode> rootNodes,
            ref float animationDuration,
            int codepage)
        #else
        private static void ReadGeometryNodes(BinaryReader reader,
            float version,
            List<CvdGeometryNode> rootNodes,
            ref float animationDuration,
            int codepage)
        #endif
        {
            CvdGeometryNode parentNode = default;

            var isGeometryNode = reader.ReadByte();
            if (isGeometryNode == 1)
            {
                parentNode = ReadGeometryNode(reader, version, ref animationDuration, codepage);
                parentNode.IsGeometryNode = true;
            }

            var numberOfChildNodes = reader.ReadInt32();

            var children = new List<CvdGeometryNode>();
            for (var i = 0; i < numberOfChildNodes; i++)
            {
                ReadGeometryNodes(reader, version, children, ref animationDuration, codepage);
            }

            parentNode.Children = children.ToArray();
            rootNodes.Add(parentNode);
        }

        #if USE_UNSAFE_BINARY_READER
        private static CvdGeometryNode ReadGeometryNode(UnsafeBinaryReader reader,
            float version,
            ref float animationDuration,
            int codepage)
        #else
        private static CvdGeometryNode ReadGeometryNode(BinaryReader reader,
            float version,
            ref float animationDuration,
            int codepage)
        #endif
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

            CvdMesh mesh = ReadMesh(reader, version, codepage);
            if (mesh.AnimationTimeKeys.Last() > animationDuration) animationDuration = mesh.AnimationTimeKeys.Last();

            var transformMatrix = new GameBoxMatrix4X4()
            {
                Xx = reader.ReadSingle(), Xy = reader.ReadSingle(), Xz = reader.ReadSingle(), Xw = reader.ReadSingle(),
                Yx = reader.ReadSingle(), Yy = reader.ReadSingle(), Yz = reader.ReadSingle(), Yw = reader.ReadSingle(),
                Zx = reader.ReadSingle(), Zy = reader.ReadSingle(), Zz = reader.ReadSingle(), Zw = reader.ReadSingle(),
                Tx = reader.ReadSingle(), Ty = reader.ReadSingle(), Tz = reader.ReadSingle(), Tw = reader.ReadSingle()
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

        #if USE_UNSAFE_BINARY_READER
        private static (CvdAnimationKeyType, byte[])[] ReadAnimationKeyInfo(UnsafeBinaryReader reader, int size)
        #else
        private static (CvdAnimationKeyType, byte[])[] ReadAnimationKeyInfo(BinaryReader reader, int size)
        #endif
        {
            var numberOfKeys = reader.ReadInt32();
            var keyInfos = new (CvdAnimationKeyType, byte[])[numberOfKeys];

            var keyType = (CvdAnimationKeyType)reader.ReadByte();

            // TODO: I have not seen type other than linear so far,
            // so I just leave the log on for now.
            if (keyType != CvdAnimationKeyType.Linear)
            {
                Debug.LogError($"Key type: {keyType.ToString()}");
            }

            for (var i = 0; i < numberOfKeys; i++)
            {
                keyInfos[i] = (keyType, reader.ReadBytes(size));
            }

            return keyInfos;
        }

        #if USE_UNSAFE_BINARY_READER
        private static CvdAnimationPositionKeyFrame[] ReadPositionAnimationKeyInfo(UnsafeBinaryReader reader, int size)
        #else
        private static CvdAnimationPositionKeyFrame[] ReadPositionAnimationKeyInfo(BinaryReader reader, int size)
        #endif
        {
            var frameInfos = ReadAnimationKeyInfo(reader, size);
            var positionKeyFrames = new CvdAnimationPositionKeyFrame[frameInfos.Length];

            for (var i = 0; i < frameInfos.Length; i++)
            {
                (CvdAnimationKeyType type, var data) = frameInfos[i];

                switch (type)
                {
                    case CvdAnimationKeyType.Tcb:
                    {
                        var positionKey = Utility.ReadStruct<CvdTcbVector3Key>(data);
                        positionKeyFrames[i] = new CvdAnimationPositionKeyFrame()
                        {
                            KeyType = type,
                            Time = positionKey.TcbKey.AnimationKey.Time,
                            Position = GameBoxInterpreter.CvdPositionToUnityPosition(positionKey.Value),
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
                            Position = GameBoxInterpreter.CvdPositionToUnityPosition(positionKey.Value),
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
                            Position = GameBoxInterpreter.CvdPositionToUnityPosition(positionKey.Value),
                        };
                        break;
                    }
                }
            }

            return positionKeyFrames;
        }

        #if USE_UNSAFE_BINARY_READER
        private static CvdAnimationRotationKeyFrame[] ReadRotationAnimationKeyInfo(UnsafeBinaryReader reader, int size)
        #else
        private static CvdAnimationRotationKeyFrame[] ReadRotationAnimationKeyInfo(BinaryReader reader, int size)
        #endif
        {
            var frameInfos = ReadAnimationKeyInfo(reader, size);
            var rotationKeyFrames = new CvdAnimationRotationKeyFrame[frameInfos.Length];

            for (var i = 0; i < frameInfos.Length; i++)
            {
                (CvdAnimationKeyType type, var data) = frameInfos[i];

                switch (type)
                {
                    case CvdAnimationKeyType.Tcb:
                    {
                        var rotationKey = Utility.ReadStruct<CvdTcbRotationKey>(data);
                        Quaternion quaternion = Quaternion.AngleAxis(rotationKey.Angle, rotationKey.Axis);
                        rotationKeyFrames[i] = new CvdAnimationRotationKeyFrame()
                        {
                            KeyType = type,
                            Time = rotationKey.TcbKey.AnimationKey.Time,
                            Rotation = GameBoxInterpreter.CvdQuaternionToUnityQuaternion(new GameBoxQuaternion()
                            {
                                X = quaternion.x,
                                Y = quaternion.y,
                                Z = quaternion.z,
                                W = quaternion.w,
                            })
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
                            Rotation = GameBoxInterpreter.CvdQuaternionToUnityQuaternion(rotationKey.Value)
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
                            Rotation = GameBoxInterpreter.CvdQuaternionToUnityQuaternion(rotationKey.Value)
                        };
                        break;
                    }
                }
            }

            return rotationKeyFrames;
        }

        #if USE_UNSAFE_BINARY_READER
        private static CvdAnimationScaleKeyFrame[] ReadScaleAnimationKeyInfo(UnsafeBinaryReader reader, int size)
        #else
        private static CvdAnimationScaleKeyFrame[] ReadScaleAnimationKeyInfo(BinaryReader reader, int size)
        #endif
        {
            var frameInfos = ReadAnimationKeyInfo(reader, size);
            var scaleKeyFrames = new CvdAnimationScaleKeyFrame[frameInfos.Length];

            for (var i = 0; i < frameInfos.Length; i++)
            {
                (CvdAnimationKeyType type, var data) = frameInfos[i];

                switch (type)
                {
                    case CvdAnimationKeyType.Tcb:
                    {
                        var scaleKey = Utility.ReadStruct<CvdTcbScaleKey>(data);
                        scaleKeyFrames[i] = new CvdAnimationScaleKeyFrame()
                        {
                            KeyType = type,
                            Time = scaleKey.TcbKey.AnimationKey.Time,
                            Scale = GameBoxInterpreter.CvdScaleToUnityScale(scaleKey.Value),
                            Rotation = GameBoxInterpreter.CvdQuaternionToUnityQuaternion(scaleKey.Rotation)
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
                            Scale = GameBoxInterpreter.CvdScaleToUnityScale(scaleKey.Value),
                            Rotation = GameBoxInterpreter.CvdQuaternionToUnityQuaternion(scaleKey.Rotation)
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
                            Scale = GameBoxInterpreter.CvdScaleToUnityScale(scaleKey.Value),
                            Rotation = GameBoxInterpreter.CvdQuaternionToUnityQuaternion(scaleKey.Rotation)
                        };
                        break;
                    }
                }
            }

            return scaleKeyFrames;
        }

        #if USE_UNSAFE_BINARY_READER
        private static CvdMesh ReadMesh(UnsafeBinaryReader reader, float version, int codepage)
        #else
        private static CvdMesh ReadMesh(BinaryReader reader, float version, int codepage)
        #endif
        {
            var numberOfFrames = reader.ReadInt32();
            var numberOfVertices = reader.ReadInt32();

            var frameVertices = new CvdVertex[numberOfFrames][];
            for (var i = 0; i < numberOfFrames; i++)
            {
                var vertices = new CvdVertex[numberOfVertices];
                for (var j = 0; j < numberOfVertices; j++)
                {
                    Vector2 uv = reader.ReadVector2();
                    Vector3 normal = GameBoxInterpreter.ToUnityNormal(reader.ReadVector3());
                    Vector3 position = GameBoxInterpreter.CvdPositionToUnityPosition(reader.ReadVector3());

                    // Quick fix for the missing/wrong normals
                    if (normal == Vector3.zero) normal = Vector3.up;

                    vertices[j] = new CvdVertex()
                    {
                        Normal = normal,
                        Position = position,
                        Uv = uv
                    };
                }
                frameVertices[i] = vertices;
            }

            var animationTimeKeys = reader.ReadSingleArray(numberOfFrames);
            for (var i = 0; i < numberOfFrames; i++)
            {
                animationTimeKeys[i] -= animationTimeKeys[0];
            }

            var numberOfMeshes = reader.ReadInt32();
            var meshSections = new CvdMeshSection[numberOfMeshes];
            for (var i = 0; i < numberOfMeshes; i++)
            {
                meshSections[i] = ReadMeshSection(reader, version, frameVertices, codepage);
            }

            return new CvdMesh()
            {
                AnimationTimeKeys = animationTimeKeys,
                MeshSections = meshSections
            };
        }

        #if USE_UNSAFE_BINARY_READER
        private static CvdMeshSection ReadMeshSection(UnsafeBinaryReader reader,
            float version,
            CvdVertex[][] allFrameVertices,
            int codepage)
        #else
        private static CvdMeshSection ReadMeshSection(BinaryReader reader,
            float version,
            CvdVertex[][] allFrameVertices,
            int codepage)
        #endif
        {
            var blendFlag = (GameBoxBlendFlag)reader.ReadByte();

            var material = new GameBoxMaterial()
            {
                Diffuse = Utility.ToColor32(reader.ReadBytes(4)),
                Ambient = Utility.ToColor32(reader.ReadBytes(4)),
                Specular = Utility.ToColor32(reader.ReadBytes(4)),
                Emissive = Utility.ToColor32(reader.ReadBytes(4)),
                Power = reader.ReadSingle()
            };

            var textureName = reader.ReadString(64, codepage);

            var numberOfIndices = reader.ReadInt32();

            var indices = new (ushort x, ushort y, ushort z)[numberOfIndices];
            for (var i = 0; i < numberOfIndices; i++)
            {
                var x = reader.ReadUInt16();
                var y = reader.ReadUInt16();
                var z = reader.ReadUInt16();
                indices[i] = (x, y, z);
            }

            var animationTimeKeys = new float[] {};
            var animationMaterials = Array.Empty<GameBoxMaterial>();
            if (version >= 0.5)
            {
                var numberOfFrames = reader.ReadInt32();

                animationTimeKeys = reader.ReadSingleArray(numberOfFrames);
                for (var i = 0; i < numberOfFrames; i++)
                {
                    animationTimeKeys[i] -= animationTimeKeys[0];
                }

                animationMaterials = new GameBoxMaterial[numberOfFrames];
                for (var i = 0; i < numberOfFrames; i++)
                {
                    animationMaterials[i] = new GameBoxMaterial()
                    {
                        Diffuse = Utility.ToColor(reader.ReadSingleArray(4)),
                        Ambient = Utility.ToColor(reader.ReadSingleArray(4)),
                        Specular = Utility.ToColor(reader.ReadSingleArray(4)),
                        Emissive = Utility.ToColor(reader.ReadSingleArray(4)),
                        Power = reader.ReadSingle()
                    };
                }
            }

            var frameVertices = new CvdVertex[allFrameVertices.Length][];

            (List<int> triangles, List<int> indexBuffer) = CalculateTriangles(indices);

            GameBoxInterpreter.ToUnityTriangles(triangles);

            for (var i = 0; i < allFrameVertices.Length; i++)
            {
                var verts = new CvdVertex[indexBuffer.Count];
                var allVertices = allFrameVertices[i];

                for (var j = 0; j < indexBuffer.Count; j++)
                {
                    verts[j] = allVertices[indexBuffer[j]];
                }

                frameVertices[i] = verts;
            }

            return new CvdMeshSection()
            {
                BlendFlag = blendFlag,
                Material = material,
                TextureName = textureName,
                FrameVertices = frameVertices,
                Triangles = triangles.ToArray(),
                AnimationTimeKeys = animationTimeKeys,
                AnimationMaterials = animationMaterials
            };
        }

        private static (List<int> triangles, List<int> indexBuffer) CalculateTriangles(
            (ushort x, ushort y, ushort z)[] allIndices)
        {
            var indexBuffer = new List<int>();
            var triangles = new List<int>();
            var index = 0;

            for (var j = 0; j < allIndices.Length; j++)
            {
                var indices = new[]
                {
                    allIndices[j].x,
                    allIndices[j].y,
                    allIndices[j].z
                };

                for (var k = 0; k < 3; k++)
                {
                    indexBuffer.Add(indices[k]);
                    triangles.Add(index++);
                }
            }

            return (triangles, indexBuffer);
        }
    }
}