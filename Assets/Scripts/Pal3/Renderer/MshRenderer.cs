// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using Core.GameBox;
using UnityEngine.UIElements;

namespace Pal3.Renderer
{
    using System;
    using Core.DataReader.Msh;
    using Core.DataReader.Mov;
    using UnityEngine;
    
    /// <summary>
    /// MSH(.msh) model renderer
    /// </summary>
    public class MshRenderer : MonoBehaviour, IDisposable
    {
        private IMaterialFactory _materialFactory;
        private Material[][] _materials;

        private MshFile _msh = null;
        
        private MovFile _mov = null;
        private float _elapsedTime = 0.0f;


        private Dictionary<string, Joint> _jointDict = new Dictionary<string, Joint>();
        
        private Material[] _skinningMaterials = null;

        public void Init(MshFile mshFile,
            IMaterialFactory materialFactory)
        {
            _msh = mshFile;
            _materialFactory = materialFactory;

            RenderSkeleton();
            RenderMesh();
        }
        
        public void PlayMov(MovFile mov)
        {
            _mov = mov;
            ClearJointTrack();
            BindJointTrack();
        }

        private void ClearJointTrack()
        {
            foreach (var joint in _jointDict)
            {
                joint.Value.track = null;
            }
        }

        private void BindJointTrack()
        {
            for(int i = 0;i < _mov.boneTrackArray.Length;i++)
            {
                var track = _mov.boneTrackArray[i];
                if (_jointDict.ContainsKey(track.boneName))
                {
                    var joint = _jointDict[track.boneName];
                    joint.track = track;
                }
            }
        }

        private void Update()
        {
            if (_mov != null)
            {
                // Get Tick Index
                _elapsedTime += Time.deltaTime;
                int tickIdx = GameBoxInterpreter.SecondToTick(_elapsedTime);
                if (tickIdx >= _mov.nDuration)
                {
                    _elapsedTime = 0;
                    tickIdx = 0;
                }
                //Debug.Log("Tick Index:" + tickIdx);
                
                UpdateJoint(tickIdx);
                UpdateSkinning();
            }
        }
        
        private void UpdateJoint(int tickIdx)
        {
            foreach (var boneName in _jointDict.Keys)
            {
                var joint = _jointDict[boneName];
                GameObject go = joint.go;
                BoneActTrack track = joint.track;

                if (track != null)
                {
                    int keyIdx = GetKeyIndex(track,tickIdx);
                    Vector3 trans = track.keyArray[keyIdx].trans;
                    Quaternion rot = track.keyArray[keyIdx].rot;
                    
                    go.transform.localPosition = trans;
                    go.transform.localRotation = rot;
                }
            }
        }

        private int GetKeyIndex(BoneActTrack track,int tickIdx)
        {
            int keyIdx = 0;
            for (keyIdx = 0;keyIdx < track.nKey;keyIdx++)
            {
                if (tickIdx < track.keyArray[keyIdx].time)
                {
                    break;
                }
            }

            if (keyIdx == 0 || keyIdx >= track.nKey)
            {
                keyIdx = track.nKey - 1;
            }

            return keyIdx;
        }


        void UpdateSkinning()
        {
            // @miao @todo
            
        }

        void RenderSkeleton()
        {
            RenderBone(_msh._boneRoot, gameObject);
        }
        
        void RenderBone(BoneNode bone,GameObject parent)
        {
            GameObject renderNode = new GameObject();
            renderNode.name = $"[bone]{bone._name}";
            renderNode.transform.SetParent(parent.transform);
            
            // display gizmo
            RenderBoneGizmo(renderNode);
            RenderBoneConnectionGizmo(parent.transform,renderNode.transform);

            // self pos rotation
            renderNode.transform.localPosition = bone._translate;
            renderNode.transform.localRotation = bone._rotate;
            
            // children
            for (int i = 0;i < bone._nChildren;i++)
            {
                BoneNode subBone = bone._children[i];
                RenderBone(subBone,renderNode);
            }
            
            // hold joint to dict
            var joint = new Joint();
            joint.go = renderNode;
            joint.track = null;
            _jointDict.Add(bone._name,joint);
        }
        
        private void RenderBoneGizmo(GameObject boneRenderNode)
        {
            var meshFilter = boneRenderNode.AddComponent<MeshFilter>();
            var meshRenderer = boneRenderNode.AddComponent<MeshRenderer>();
            
            var mesh = new UnityEngine.Mesh();
            mesh.SetVertices(BuildBoneGizmoMesh());
            mesh.SetTriangles(BuildBoneGizmoTriangle(),0);
            meshFilter.sharedMesh = mesh;
            
            // material
            Material material = _materialFactory.CreateBoneGizmoMaterial();
            if(material != null)
            {
                meshRenderer.sharedMaterial = material;
                material.renderQueue = 5000;    // at last 
            }
        }

        void RenderBoneConnectionGizmo(Transform from,Transform to)
        {
            //Vector3 fromTo = to.position - from.position;
            
            
        }
        
        void RenderMesh()
        {
            if (_msh._nSubMesh > 0)
            {
                _skinningMaterials = new Material[_msh._nSubMesh];
            }

            for (int i = 0; i < _msh._nSubMesh; i++)
            {
                SubMesh subMesh = _msh._subMeshArray[i];
                RenderSubMesh(subMesh, i);
            }
        }

        void RenderSubMesh(SubMesh subMesh, int subMeshIndex)
        {
            GameObject subMeshNode = new GameObject($"[submesh] {subMeshIndex}");
            subMeshNode.transform.SetParent(gameObject.transform);

            var meshRenderer = subMeshNode.AddComponent<MeshRenderer>();
            var meshFilter = subMeshNode.AddComponent<MeshFilter>();
            
            var mesh = new UnityEngine.Mesh();
            mesh.MarkDynamic(); // @temp
            mesh.SetVertices(BuildVerts(subMesh));
            mesh.SetTriangles(BuildTriangles(subMesh),subMeshIndex);
            
            mesh.SetUVs(1,BuildBoneIds(subMesh));
            mesh.SetUVs(2, BuildBoneWeights(subMesh));

            meshFilter.sharedMesh = mesh;
            
            
                
            // hold material
            var material = _materialFactory.CreateSkinningMaterial();
            meshRenderer.sharedMaterial = material;
            _skinningMaterials[subMeshIndex] = material;
        }

        Vector3[] BuildVerts(SubMesh subMesh)
        {
            Vector3[] verts = new Vector3[subMesh._verts.Length];
            for (int i = 0; i < subMesh._verts.Length; i++)
            {
                verts[i] = subMesh._verts[i].pos;
            }

            return verts;
        }

        int[] BuildTriangles(SubMesh subMesh)
        {
            int len = subMesh._faces.Length * 3;
            int[] triangles = new int[len];

            int idx = 0;
            for (int i = 0;i < subMesh._faces.Length;i++)
            {
                triangles[idx++] = subMesh._faces[i].vertIndex[0];
                triangles[idx++] = subMesh._faces[i].vertIndex[1];
                triangles[idx++] = subMesh._faces[i].vertIndex[2];
            }

            return triangles;
        }

        List<Vector4> BuildBoneWeights(SubMesh subMesh)
        {
            List<Vector4> weights = new List<Vector4>();
            for (int i = 0;i < subMesh._verts.Length;i++)
            {
                var vert = subMesh._verts[i];
                var weight = new Vector4(vert.weights[0], vert.weights[1], vert.weights[2], vert.weights[3]);
                //var r = new System.Random();
                //var weight = new Vector4((float)r.NextDouble(),(float)r.NextDouble(),(float)r.NextDouble(),1.0f);
                weights.Add(weight);
            }
            return weights;
        }
        
        List<Vector4> BuildBoneIds(SubMesh subMesh)
        {
            List<Vector4> ids = new List<Vector4>();
            for (int i = 0;i < subMesh._verts.Length;i++)
            {
                var vert = subMesh._verts[i];
                ids.Add(new Vector4(vert.boneIds[0],vert.boneIds[1],vert.boneIds[2],vert.boneIds[3]));
            }
            return ids;
        }
        
        
        /*
        BoneWeight[] BuildBoneWeights(SubMesh subMesh)
        {
            BoneWeight[] boneWeight = new BoneWeight[subMesh._verts.Length];
            for (int i = 0; i < subMesh._verts.Length; i++)
            {
                PhyVertex vert = subMesh._verts[i];
                
                boneWeight[i] = new BoneWeight();
                boneWeight[i].weight0 = vert.weights[0];
                boneWeight[i].weight1 = vert.weights[1];
                boneWeight[i].weight2 = vert.weights[2];
                boneWeight[i].weight3 = vert.weights[3];
                boneWeight[i].boneIndex0 = vert.boneIds[0];
                boneWeight[i].boneIndex1 = vert.boneIds[1];
                boneWeight[i].boneIndex2 = vert.boneIds[2];
                boneWeight[i].boneIndex3 = vert.boneIds[3];
            }
            return boneWeight;
        }
        */
        
        Vector3[] BuildBoneGizmoMesh()
        {
            const float size = 0.02f;
            Vector3[] verts = new Vector3[8];
            verts[0] = new Vector3(-size,-size,-size);
            verts[1] = new Vector3(-size, size,-size);
            verts[2] = new Vector3( size,-size,-size);
            verts[3] = new Vector3( size, size,-size);
            
            verts[4] = new Vector3(-size,-size,size);
            verts[5] = new Vector3(-size, size,size);
            verts[6] = new Vector3( size,-size,size);
            verts[7] = new Vector3( size,-size,size);
            
            return verts;
        }

        int[] BuildBoneGizmoTriangle()
        {
            int[] indices =
            {
                0,1,2, 2,1,3,
                4,5,6, 6,5,7,

                4,5,0, 0,5,1,
                2,4,6, 6,4,7,

                1,5,3, 3,5,7,
                0,4,2, 2,4,6,
            };
            return indices;
        }

        private void OnDisable()
        {
            Dispose();
        }

        public void Dispose()
        {
            
        }
    }
    
    class Joint
    {
        public string name;
        public GameObject go = null;
        public BoneActTrack track = null;
    }
}