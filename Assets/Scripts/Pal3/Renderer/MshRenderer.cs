// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Renderer
{
    using System;
    using System.Collections;
    using System.Linq;
    using System.Threading;
    using Core.DataLoader;
    using Core.DataReader.Mv3;
    using Core.DataReader.Pol;
    using Core.DataReader.Msh;
    using Core.GameBox;
    using Core.Renderer;
    using Core.Utils;
    using Dev;
    using UnityEngine;

    /// <summary>
    /// MSH(.msh) model renderer
    /// </summary>
    public class MshRenderer : MonoBehaviour, IDisposable
    {
        private IMaterialFactory _materialFactory;
        private Material[][] _materials;

        private MshFile _msh = null;

        private static GameObject sBoneGizmoPrefab = null;

        public void Init(MshFile mshFile,
            IMaterialFactory materialFactory)
        {
            _msh = mshFile;
            _materialFactory = materialFactory;

            RenderSkeleton();
            RenderMesh();
        }
        
        /*
        private void Update()
        {
            Debug.DrawLine(new Vector3(0, 0, 0), new Vector3(100, 100, 100), new Color(1.0f, 1.0f, 0.0f));
        }
        */

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
            meshFilter.sharedMesh = mesh;
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
            // DisposeAnimation();
            //
            // if (_renderMeshComponents != null)
            // {
            //     foreach (RenderMeshComponent renderMeshComponent in _renderMeshComponents)
            //     {
            //         Destroy(renderMeshComponent.Mesh);
            //         Destroy(renderMeshComponent.MeshRenderer);
            //     }
            //
            //     _renderMeshComponents = null;
            // }
            //
            // if (_meshObjects != null)
            // {
            //     foreach (GameObject meshObject in _meshObjects)
            //     {
            //         Destroy(meshObject);
            //     }
            //
            //     _meshObjects = null;
            // }
        }
    }
}