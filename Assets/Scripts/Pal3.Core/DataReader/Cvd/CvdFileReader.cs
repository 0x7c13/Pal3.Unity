// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Core.DataReader.Cvd
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Runtime.InteropServices;
    using Primitives;
    using Utilities;

    public sealed class CvdFileReader : IFileReader<CvdFile>
    {
        public CvdFile Read(IBinaryReader reader, int codepage)
        {
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

        private static void ReadGeometryNodes(IBinaryReader reader,
            float version,
            List<CvdGeometryNode> rootNodes,
            ref float animationDuration,
            int codepage)
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

        private static CvdGeometryNode ReadGeometryNode(IBinaryReader reader,
            float version,
            ref float animationDuration,
            int codepage)
        {
            var positionKeySize = Math.Max(Marshal.SizeOf(typeof(CvdTcbVector3Key)),
                Math.Max(Marshal.SizeOf(typeof(CvdBezierVector3Key)),
                    Marshal.SizeOf(typeof(CvdLinearVector3Key))));

            var positionKeyInfos = ReadPositionAnimationKeyInfo(reader, positionKeySize);
            if (positionKeyInfos[^1].Time > animationDuration) animationDuration = positionKeyInfos[^1].Time;

            var rotationKeySize = Math.Max(Marshal.SizeOf(typeof(CvdTcbRotationKey)),
                Math.Max(Marshal.SizeOf(typeof(CvdBezierRotationKey)),
                Marshal.SizeOf(typeof(CvdLinearRotationKey))));

            var rotationKeyInfos = ReadRotationAnimationKeyInfo(reader, rotationKeySize);
            if (rotationKeyInfos[^1].Time > animationDuration) animationDuration = rotationKeyInfos[^1].Time;

            var scaleKeySize = Math.Max(Marshal.SizeOf(typeof(CvdTcbScaleKey)),
                Math.Max(Marshal.SizeOf(typeof(CvdBezierScaleKey)),
                Marshal.SizeOf(typeof(CvdLinearScaleKey))));

            var scaleKeyInfos = ReadScaleAnimationKeyInfo(reader, scaleKeySize);
            if (scaleKeyInfos[^1].Time > animationDuration) animationDuration = scaleKeyInfos[^1].Time;

            var scale = reader.ReadSingle();

            CvdMesh mesh = ReadMesh(reader, version, codepage);
            if (mesh.AnimationTimeKeys[^1] > animationDuration) animationDuration = mesh.AnimationTimeKeys[^1];

            var transformMatrix = new GameBoxMatrix4x4()
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

        private static (CvdAnimationKeyType, byte[])[] ReadAnimationKeyInfo(IBinaryReader reader, int size)
        {
            var numberOfKeys = reader.ReadInt32();
            var keyInfos = new (CvdAnimationKeyType, byte[])[numberOfKeys];

            var keyType = (CvdAnimationKeyType)reader.ReadByte();

            for (var i = 0; i < numberOfKeys; i++)
            {
                keyInfos[i] = (keyType, reader.ReadBytes(size));
            }

            return keyInfos;
        }

        private static CvdAnimationPositionKeyFrame[] ReadPositionAnimationKeyInfo(IBinaryReader reader, int size)
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
                        var positionKey = CoreUtility.ReadStruct<CvdTcbVector3Key>(data);
                        positionKeyFrames[i] = new CvdAnimationPositionKeyFrame()
                        {
                            KeyType = type,
                            Time = positionKey.TcbKey.AnimationKey.Time,
                            GameBoxPosition = positionKey.Value,
                        };
                        break;
                    }
                    case CvdAnimationKeyType.Bezier:
                    {
                        var positionKey = CoreUtility.ReadStruct<CvdBezierVector3Key>(data);
                        positionKeyFrames[i] = new CvdAnimationPositionKeyFrame()
                        {
                            KeyType = type,
                            Time = positionKey.AnimationKey.Time,
                            GameBoxPosition = positionKey.Value,
                        };
                        break;
                    }
                    case CvdAnimationKeyType.Linear:
                    {
                        var positionKey = CoreUtility.ReadStruct<CvdLinearVector3Key>(data);
                        positionKeyFrames[i] = new CvdAnimationPositionKeyFrame()
                        {
                            KeyType = type,
                            Time = positionKey.AnimationKey.Time,
                            GameBoxPosition = positionKey.Value,
                        };
                        break;
                    }
                }
            }

            return positionKeyFrames;
        }

        private static CvdAnimationRotationKeyFrame[] ReadRotationAnimationKeyInfo(IBinaryReader reader, int size)
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
                        var rotationKey = CoreUtility.ReadStruct<CvdTcbRotationKey>(data);
                        GameBoxQuaternion quaternion = GameBoxQuaternion.AngleAxis(rotationKey.Angle, rotationKey.Axis);
                        rotationKeyFrames[i] = new CvdAnimationRotationKeyFrame()
                        {
                            KeyType = type,
                            Time = rotationKey.TcbKey.AnimationKey.Time,
                            GameBoxRotation = new GameBoxQuaternion()
                            {
                                X = quaternion.X,
                                Y = quaternion.Y,
                                Z = quaternion.Z,
                                W = quaternion.W,
                            }
                        };
                        break;
                    }
                    case CvdAnimationKeyType.Bezier:
                    {
                        var rotationKey = CoreUtility.ReadStruct<CvdBezierRotationKey>(data);
                        rotationKeyFrames[i] = new CvdAnimationRotationKeyFrame()
                        {
                            KeyType = type,
                            Time = rotationKey.AnimationKey.Time,
                            GameBoxRotation = rotationKey.Value
                        };
                        break;
                    }
                    case CvdAnimationKeyType.Linear:
                    {
                        var rotationKey = CoreUtility.ReadStruct<CvdLinearRotationKey>(data);
                        rotationKeyFrames[i] = new CvdAnimationRotationKeyFrame()
                        {
                            KeyType = type,
                            Time = rotationKey.AnimationKey.Time,
                            GameBoxRotation = rotationKey.Value
                        };
                        break;
                    }
                }
            }

            return rotationKeyFrames;
        }

        private static CvdAnimationScaleKeyFrame[] ReadScaleAnimationKeyInfo(IBinaryReader reader, int size)
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
                        var scaleKey = CoreUtility.ReadStruct<CvdTcbScaleKey>(data);
                        scaleKeyFrames[i] = new CvdAnimationScaleKeyFrame()
                        {
                            KeyType = type,
                            Time = scaleKey.TcbKey.AnimationKey.Time,
                            GameBoxScale = scaleKey.Value,
                            GameBoxRotation = scaleKey.Rotation
                        };
                        break;
                    }
                    case CvdAnimationKeyType.Bezier:
                    {
                        var scaleKey = CoreUtility.ReadStruct<CvdBezierScaleKey>(data);
                        scaleKeyFrames[i] = new CvdAnimationScaleKeyFrame()
                        {
                            KeyType = type,
                            Time = scaleKey.AnimationKey.Time,
                            GameBoxScale = scaleKey.Value,
                            GameBoxRotation = scaleKey.Rotation
                        };
                        break;
                    }
                    case CvdAnimationKeyType.Linear:
                    {
                        var scaleKey = CoreUtility.ReadStruct<CvdLinearScaleKey>(data);
                        scaleKeyFrames[i] = new CvdAnimationScaleKeyFrame()
                        {
                            KeyType = type,
                            Time = scaleKey.AnimationKey.Time,
                            GameBoxScale = scaleKey.Value,
                            GameBoxRotation = scaleKey.Rotation
                        };
                        break;
                    }
                }
            }

            return scaleKeyFrames;
        }

        private static CvdMesh ReadMesh(IBinaryReader reader, float version, int codepage)
        {
            var numberOfFrames = reader.ReadInt32();
            var numberOfVertices = reader.ReadInt32();

            var frameVertices = new CvdVertex[numberOfFrames][];
            for (var i = 0; i < numberOfFrames; i++)
            {
                var vertices = new CvdVertex[numberOfVertices];
                for (var j = 0; j < numberOfVertices; j++)
                {
                    GameBoxVector2 uv = reader.ReadGameBoxVector2();
                    GameBoxVector3 normal = reader.ReadGameBoxVector3();
                    GameBoxVector3 position = reader.ReadGameBoxVector3();

                    // Quick fix for the missing/wrong normals
                    if (normal == GameBoxVector3.Zero)
                    {
                        normal = new GameBoxVector3(0, 1, 0); // Up
                    }

                    vertices[j] = new CvdVertex()
                    {
                        GameBoxNormal = normal,
                        GameBoxPosition = position,
                        Uv = uv
                    };
                }
                frameVertices[i] = vertices;
            }

            var animationTimeKeys = reader.ReadSingles(numberOfFrames);
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

        private static CvdMeshSection ReadMeshSection(IBinaryReader reader,
            float version,
            CvdVertex[][] allFrameVertices,
            int codepage)
        {
            GameBoxBlendFlag blendFlag = (GameBoxBlendFlag)reader.ReadByte();

            GameBoxMaterial material = new ()
            {
                Diffuse = (Color)reader.ReadColor32(),
                Ambient = (Color)reader.ReadColor32(),
                Specular = (Color)reader.ReadColor32(),
                Emissive = (Color)reader.ReadColor32(),
                SpecularPower = reader.ReadSingle(),
                TextureFileNames = new [] { reader.ReadString(64, codepage) }
            };

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

                animationTimeKeys = reader.ReadSingles(numberOfFrames);
                for (var i = 0; i < numberOfFrames; i++)
                {
                    animationTimeKeys[i] -= animationTimeKeys[0];
                }

                animationMaterials = new GameBoxMaterial[numberOfFrames];
                for (var i = 0; i < numberOfFrames; i++)
                {
                    animationMaterials[i] = new GameBoxMaterial()
                    {
                        Diffuse = reader.ReadColor(),
                        Ambient = reader.ReadColor(),
                        Specular = reader.ReadColor(),
                        Emissive = reader.ReadColor(),
                        SpecularPower = reader.ReadSingle()
                    };
                }
            }

            var frameVertices = new CvdVertex[allFrameVertices.Length][];

            (int[] triangles, int[] indexBuffer) = CalculateTriangles(indices);

            for (var i = 0; i < allFrameVertices.Length; i++)
            {
                var verts = new CvdVertex[indexBuffer.Length];
                var allVertices = allFrameVertices[i];

                for (var j = 0; j < indexBuffer.Length; j++)
                {
                    verts[j] = allVertices[indexBuffer[j]];
                }

                frameVertices[i] = verts;
            }

            return new CvdMeshSection()
            {
                BlendFlag = blendFlag,
                Material = material,
                FrameVertices = frameVertices,
                GameBoxTriangles = triangles,
                AnimationTimeKeys = animationTimeKeys,
                AnimationMaterials = animationMaterials
            };
        }

        private static (int[] triangles, int[] indexBuffer) CalculateTriangles(
            (ushort x, ushort y, ushort z)[] allIndices)
        {
            var indexBuffer = new int[allIndices.Length * 3];
            var triangles = new int[allIndices.Length * 3];
            int index = 0;

            for (var i = 0; i < allIndices.Length; i++)
            {
                ushort[] indices =
                {
                    allIndices[i].x,
                    allIndices[i].y,
                    allIndices[i].z
                };

                for (var j = 0; j < 3; j++)
                {
                    indexBuffer[index] = indices[j];
                    triangles[index] = index;
                    index++;
                }
            }

            return (triangles, indexBuffer);
        }
    }
}