// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

#define USE_UNSAFE_BINARY_READER

using System;
using UnityEngine.Assertions;
using UnityEngine.PlayerLoop;

namespace Core.DataReader.Msh
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Extensions;
    using GameBox;
    using UnityEngine;
    using Utils;

    public static class MshFileReader
    {
        public static MshFile Read(byte[] data, int codepage)
        {
#if USE_UNSAFE_BINARY_READER
            using var reader = new UnsafeBinaryReader(data);
#else
            using var stream = new MemoryStream(data);
            using var reader = new BinaryReader(stream);
#endif  
         
            MshFile result = new MshFile();

            ReadHeader(result,reader);
            ReadSkeleton(result, reader,codepage);
            ReadSubMeshes(result,reader,codepage);
            
            return result;
        }

        private static void ReadHeader(MshFile mshFile,UnsafeBinaryReader reader)
        {
            var chars = reader.ReadChars(4);
            mshFile._header.Magic = new string(chars[..^1]);
            if (mshFile._header.Magic.ToLower() != "msh")
            {
                throw new InvalidDataException("Invalid MSH(.msh) file: header != MSH");
            }

            mshFile._header.Version = reader.ReadInt32();
            if (mshFile._header.Version != 100)
            {
                throw new InvalidDataException("Invalid MSH(.msh) file: version != 100");
            }

            mshFile._header.SkeletonOff = reader.ReadInt32();
            mshFile._header.MeshOff = reader.ReadInt32();
        }
        
        private static void ReadSkeleton(MshFile mshFile,UnsafeBinaryReader reader,int codepage)
        {
            BoneNode boneNode = new BoneNode();
            boneNode._name = "root";
            boneNode._nChildren = reader.ReadInt32();
            
            mshFile._boneRoot = boneNode;
            mshFile._boneRoot._children = null;
            if (boneNode._nChildren > 0)
            {
                mshFile._boneRoot._children = new BoneNode[boneNode._nChildren];
                for (int i = 0;i < boneNode._nChildren;i++)
                {
                    BoneNode subBone = new BoneNode();
                    ReadBoneNode(reader,codepage,subBone,boneNode);
                    mshFile._boneRoot._children[i] = subBone;
                }   
            }
        }
        
        private static void ReadBoneNode(UnsafeBinaryReader reader,int codepage,BoneNode boneNode,BoneNode parent)
        {
            // type
            BoneNode.Type eType = (BoneNode.Type)(reader.ReadInt32());
            boneNode._type = eType;

            // name
            int nameLen = reader.ReadInt32();
            string boneName = reader.ReadString(nameLen,codepage);//new string(reader.ReadChars(nameLen));
            boneNode._name = boneName;
            
            // Check Valid
            if (boneNode._type != BoneNode.Type.BONE)
            {
                Debug.LogWarning("Not Supported BoneNode Type:" + boneNode._type + " bone name:" + boneNode._name);                
            }
            

            // translate,rotate,scale,flip
            boneNode._translate = GameBoxInterpreter.ToUnityPosition(reader.ReadVector3());
            boneNode._rotate = GameBoxInterpreter.MshQuaternionToUnityQuaternion(new GameBoxQuaternion()
            {
                X = reader.ReadSingle(),
                Y = reader.ReadSingle(),
                Z = reader.ReadSingle(),
                W = reader.ReadSingle(),
            });
            // assert rotate is unit
            boneNode._scale = GameBoxInterpreter.ToUnityMatrix4x4(new GameBoxMatrix4X4()
            {
                Xx = reader.ReadSingle(), Xy = reader.ReadSingle(), Xz = reader.ReadSingle(), Xw = 0,
                Yx = reader.ReadSingle(), Yy = reader.ReadSingle(), Yz = reader.ReadSingle(), Yw = 0,
                Zx = reader.ReadSingle(), Zy = reader.ReadSingle(), Zz = reader.ReadSingle(), Zw = 0,
                Tx = 0, Ty = 0, Tz = 0, Tw = 1,
            });
            
            boneNode._flipScale = reader.ReadSingle();
            if (Math.Abs(boneNode._flipScale - 1.0f) >= 0.0001f)
            {
                Debug.LogWarning("Flip Must Nearly == 1,name:" + boneNode._name + " flip:" + boneNode._flipScale);                
            }

            
            boneNode._localXForm = GameBoxInterpreter.ToUnityMatrix4x4(new GameBoxMatrix4X4()
            {
                Xx = reader.ReadSingle(), Xy = reader.ReadSingle(), Xz = reader.ReadSingle(), Xw = reader.ReadSingle(),
                Yx = reader.ReadSingle(), Yy = reader.ReadSingle(), Yz = reader.ReadSingle(), Yw = reader.ReadSingle(),
                Zx = reader.ReadSingle(), Zy = reader.ReadSingle(), Zz = reader.ReadSingle(), Zw = reader.ReadSingle(),
                Tx = reader.ReadSingle(), Ty = reader.ReadSingle(), Tz = reader.ReadSingle(), Tw = reader.ReadSingle()
            });
            
            // read IDs
            boneNode._selfID = reader.ReadInt32();
            boneNode._parentID = reader.ReadInt32();
            boneNode._nChildren = reader.ReadInt32();
            boneNode._parent = parent;

            if (boneNode._nChildren > 0)
            {
                boneNode._children = new BoneNode[boneNode._nChildren];
                for (int i = 0;i < boneNode._nChildren;i++)
                {
                    BoneNode subNode = new BoneNode();
                    ReadBoneNode(reader, codepage, subNode, boneNode);
                    boneNode._children[i] = subNode;
                }    
            }
        }

        private static void ReadSubMeshes(MshFile mshFile,UnsafeBinaryReader reader,int codepage)
        {
            mshFile._nSubMesh = reader.ReadInt32();
            if (mshFile._nSubMesh > 0)
            {
                mshFile._subMeshArray = new SubMesh[mshFile._nSubMesh];
                for (int i = 0;i < mshFile._nSubMesh;i++)
                {
                    SubMesh subMesh = new SubMesh();
                    mshFile._subMeshArray[i] = subMesh;
                    ReadOneSubMesh(reader,codepage,subMesh);
                }
            }
        }
        
        private static void ReadOneSubMesh(UnsafeBinaryReader reader,int codepage,SubMesh subMesh)
        {
            subMesh._mtrlID = reader.ReadInt32();
            subMesh._nVert = reader.ReadInt32();
            subMesh._nFace = reader.ReadInt32();
            subMesh._nSpring = reader.ReadInt32();
            subMesh._nLodStep = reader.ReadInt32();
            subMesh._layerFlag = reader.ReadInt32();

            Debug.Assert(subMesh._nVert > 0 && subMesh._nFace > 0,"sub mesh vert face num invalid");
            subMesh._verts = new PhyVertex[subMesh._nVert];
            subMesh._faces = new PhyFace[subMesh._nFace];
            
            // read verts
            if ((subMesh._layerFlag & SubMesh.MESH_LAYER_XYZ) != 0)
            {
                for (int i = 0;i < subMesh._nVert;i++)
                {
                    PhyVertex vert = new PhyVertex();
                    vert.pos = GameBoxInterpreter.ToUnityPosition(reader.ReadVector3());
                    subMesh._verts[i] = vert;
                }
            }

            if ((subMesh._layerFlag & SubMesh.MESH_LAYER_LOD) != 0)
            {
                // do nothing
            }

            if ((subMesh._layerFlag & SubMesh.MESH_LAYER_BONE) != 0)
            {
                for (int i = 0;i < subMesh._nVert;i++)
                {
                    PhyVertex vert = subMesh._verts[i];
                    vert.numBone = reader.ReadByte();
                    for (int j = 0;j < vert.numBone;j++)
                    {
                        vert.boneIds[j] = reader.ReadChars(1)[0];
                        vert.weights[j] = reader.ReadChars(1)[0];
                    }
                }
            }

            if ((subMesh._layerFlag & SubMesh.MESH_LAYER_PHYSICS) != 0)
            {
                Debug.Assert(false,"MESH_LAYER_PHYSICS");
            }
            
            // read faces
            for (int i = 0;i < subMesh._nFace;i++)
            {
                PhyFace face = new PhyFace();
                subMesh._faces[i] = face;

                face.vertIndex[0] = reader.ReadInt32();
                face.vertIndex[1] = reader.ReadInt32();
                face.vertIndex[2] = reader.ReadInt32();
                
                face.norm[0] = reader.ReadInt16();
                face.norm[1] = reader.ReadInt16();
                face.norm[2] = reader.ReadInt16();
                face.norm[3] = reader.ReadInt16();
                
                face.u[0,0] = reader.ReadSingle();
                face.u[0,1] = reader.ReadSingle();
                face.u[1,0] = reader.ReadSingle();
                face.u[1,1] = reader.ReadSingle();
                face.u[2,0] = reader.ReadSingle();
                face.u[2,1] = reader.ReadSingle();

                face.v[0,0] = reader.ReadSingle();
                face.v[0,1] = reader.ReadSingle();
                face.v[1,0] = reader.ReadSingle();
                face.v[1,1] = reader.ReadSingle();
                face.v[2,0] = reader.ReadSingle();
                face.v[2,1] = reader.ReadSingle();
            }
                
            // read spring
            if (subMesh._nSpring > 0)
            {
                Debug.Assert(false,"didn't handle springs");
            }

        }
    }
}