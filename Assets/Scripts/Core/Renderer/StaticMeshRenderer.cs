// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Core.Renderer
{
    using UnityEngine;
    using UnityEngine.Rendering;

    public sealed class StaticMeshRenderer : MonoBehaviour
    {
        private MeshRenderer _meshRenderer;
        private MeshFilter _meshFilter;

        public Mesh Render(ref Vector3[] vertices,
            ref int[] triangles,
            ref Vector3[] normals,
            ref Vector2[] uv,
            ref Material material,
            bool isDynamic)
        {
            Dispose();

            _meshRenderer = gameObject.AddComponent<MeshRenderer>();
            //_meshRenderer.receiveShadows = false;
            //_meshRenderer.lightProbeUsage = LightProbeUsage.Off;
            _meshRenderer.sharedMaterial = material;

            _meshFilter = gameObject.AddComponent<MeshFilter>();

            var mesh = new Mesh();
            if (isDynamic)
            {
                mesh.MarkDynamic();
            }

            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uv);

            if (triangles.Length == 0)
            {
                // https://docs.unity3d.com/ScriptReference/Mesh.RecalculateBounds.html
                // After modifying vertices you should call this function to ensure the
                // bounding volume is correct. Assigning triangles automatically
                // recalculates the bounding volume.
                mesh.RecalculateBounds();
            }

            _meshFilter.sharedMesh = mesh;

            return mesh;
        }

        /*
        public Mesh Render(ref Vector3[] vertices,
            ref int[] triangles,
            ref Vector3[] normals,
            ref Vector2[] mainTextureUv,
            ref Vector2[] secondaryTextureUv,
            ref Material material,
            bool isDynamic)
        {
            Mesh mesh = RenderInternal(ref vertices,ref triangles,ref normals,ref mainTextureUv,ref secondaryTextureUv,isDynamic);
            _meshRenderer.sharedMaterial = material;
            return mesh;
        }
        */
        
        public Mesh RenderWithMaterials(ref Vector3[] vertices,
            ref int[] triangles,
            ref Vector3[] normals,
            ref Vector2[] mainTextureUv,
            ref Vector2[] secondaryTextureUv,
            ref Material[] materials,
            bool isDynamic)
        {
            Mesh mesh = RenderInternal(ref vertices,ref triangles,ref normals,ref mainTextureUv,ref secondaryTextureUv,isDynamic);
            _meshRenderer.materials = materials;
            
            return mesh;
        }

        private Mesh RenderInternal(ref Vector3[] vertices,
            ref int[] triangles,
            ref Vector3[] normals,
            ref Vector2[] mainTextureUv,
            ref Vector2[] secondaryTextureUv,
            bool isDynamic)
        {
            Dispose();

            _meshRenderer = gameObject.AddComponent<MeshRenderer>();
            //_meshRenderer.receiveShadows = false;
            //_meshRenderer.lightProbeUsage = LightProbeUsage.Off;
            //_meshRenderer.sharedMaterial = material;
            

            _meshFilter = gameObject.AddComponent<MeshFilter>();
            var mesh = new Mesh();
            if (isDynamic)
            {
                mesh.MarkDynamic();
            }

            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, mainTextureUv);
            mesh.SetUVs(1, secondaryTextureUv);

            if (triangles.Length == 0)
            {
                // https://docs.unity3d.com/ScriptReference/Mesh.RecalculateBounds.html
                // After modifying vertices you should call this function to ensure the
                // bounding volume is correct. Assigning triangles automatically
                // recalculates the bounding volume.
                mesh.RecalculateBounds();
            }

            _meshFilter.sharedMesh = mesh;

            return mesh;
        }
    

        public Mesh GetMesh()
        {
            return _meshFilter == null ? null : _meshFilter.sharedMesh;
        }

        public bool IsVisible()
        {
            return _meshRenderer != null && _meshRenderer.isVisible;
        }

        public Bounds GetRendererBounds()
        {
            return _meshRenderer.bounds;
        }

        public Bounds GetMeshBounds()
        {
            return _meshFilter.sharedMesh.bounds;
        }

        private void OnDisable()
        {
            Dispose();
        }

        private void Dispose()
        {
            if (_meshFilter != null)
            {
                Destroy(_meshFilter.sharedMesh);
                Destroy(_meshFilter);
            }

            if (_meshRenderer != null)
            {
                for (int i = 0;i < _meshRenderer.materials.Length;i++)
                {
                    Destroy(_meshRenderer.materials[i]);
                }
                Destroy(_meshRenderer);
            }
        }
    }
}