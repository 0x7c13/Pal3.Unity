// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Core.DataReader.Msh
{
    using GameBox;
    using UnityEngine;


    public class MshFile
    {
        // header
        public MshHeader _header;
        
        // skeleton
        public BoneNode _boneRoot = null;
        
        // submesh
        public int _nSubMesh;
        public SubMesh[] _subMeshArray = null;

    }

    public struct MshHeader
    {
        public string Magic;
        public int Version;
        public int SkeletonOff;
        public int MeshOff;
    }

    public class BoneNode
    {
        public enum Type
        {
            NORMAL = 0,
            BONE,
            DUMMY
        }

        public string _name;
        public Type _type;
        public int _nChildren = 0;
        public BoneNode[] _children = null;
        public BoneNode _parent = null;

        // SQT
        public Vector3 _translate;
        public Quaternion _rotate;
        //public float[,] _scale = new float[3, 3];
        public Matrix4x4 _scale; // only need 3x3
        
        public float _flipScale;
        public Matrix4x4 _localXForm;    // transform from model space to bone space
        
        
        // IDs
        public int _selfID;
        public int _parentID;
        
        // todo
        // reference to gbbnactor.h - class gbBoneNode
    }

    public class SubMesh
    {
        public static int MESH_LAYER_XYZ     = 1;
        public static int MESH_LAYER_NORMAL  = 2;
        public static int MESH_LAYER_TEX1    = 4;
        public static int MESH_LAYER_TEX2    = 8;
        public static int MESH_LAYER_TEX3    = 16;
        public static int MESH_LAYER_TEX4    = 32;
        public static int MESH_LAYER_TEX5    = 64;
        public static int MESH_LAYER_TEX6    = 128;
        public static int MESH_LAYER_TEX7    = 256;
        public static int MESH_LAYER_TEX8    = 512;
        public static int MESH_LAYER_LOD     = 1024;
        public static int MESH_LAYER_BONE    = 2048;
        public static int MESH_LAYER_PHYSICS = 4096;
        
        // base info
        public int _mtrlID;
        public int _nVert = 0;
        public int _nFace = 0;
        public int _nSpring = 0;
        public int _nLodStep = 0;
        public int _layerFlag = 0;
        
        // vert & face
        public PhyVertex[] _verts = null;
        public PhyFace[] _faces = null;
    }
    
    // reference gbbnactor.h struct gbPhysVertex
    public class PhyVertex
    {
        public static int MAX_BLEND_BONE = 8;
        
        // vertex pos
        public Vector3 pos;
        
        // bone params
        public int[] boneIds = new int[MAX_BLEND_BONE];
        public int[] weights = new int[MAX_BLEND_BONE];
        public int numBone;     // influenced by how many bones
    }

    // reference gbbnactor.h struct gbPhysFace
    public class PhyFace
    {
        public int[] vertIndex = new int[3];    // One face composed by 3 verts
        public int[] norm = new int[4]; // 
        public float[,] u = new float[3, 2];    
        public float[,] v = new float[3, 2];

    }
}
