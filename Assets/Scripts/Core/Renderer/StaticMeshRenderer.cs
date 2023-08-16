// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Core.Renderer
{
    using System;
    using UnityEngine;

    public sealed class StaticMeshRenderer : MonoBehaviour, IDisposable
    {
        private MeshRenderer _meshRenderer;
        private MeshFilter _meshFilter;

        private Material[] _materials;

        public Mesh Render(ref Vector3[] vertices,
            ref int[] triangles,
            ref Vector3[] normals,
            ref Vector2[] mainTextureUvs,
            ref Vector2[] secondaryTextureUvs,
            ref Material[] materials,
            bool isDynamic)
        {
            Dispose();

            _materials = materials;
            _meshRenderer = gameObject.AddComponent<MeshRenderer>();
            _meshRenderer.sharedMaterials = materials;

            _meshFilter = gameObject.AddComponent<MeshFilter>();
            var mesh = new Mesh();
            if (isDynamic)
            {
                mesh.MarkDynamic();
            }

            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, mainTextureUvs);
            mesh.SetUVs(1, secondaryTextureUvs);

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

        public Material[] GetMaterials()
        {
            return _materials;
        }

        private void OnDisable()
        {
            Dispose();
        }

        public void Dispose()
        {
            if (_meshFilter != null)
            {
                Destroy(_meshFilter.sharedMesh);
                Destroy(_meshFilter);
            }

            if (_meshRenderer != null)
            {
                Destroy(_meshRenderer);
            }

            _materials = null;
        }
    }
}