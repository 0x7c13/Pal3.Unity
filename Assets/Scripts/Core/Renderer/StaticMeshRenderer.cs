// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Core.Renderer
{
    using System;
    using UnityEngine;

    public class StaticMeshRenderer : MonoBehaviour
    {
        private MeshRenderer _meshRenderer;
        private MeshFilter _meshFilter;

        public Mesh Render(Vector3[] vertices,
            int[] triangles,
            Vector3[] normals,
            Vector2[] uv,
            Material material,
            bool isDynamic)
        {
            Dispose();

            _meshRenderer = gameObject.AddComponent<MeshRenderer>();
            _meshRenderer.sharedMaterial = material;
            _meshFilter = gameObject.AddComponent<MeshFilter>();

            var mesh = new Mesh();
            if (isDynamic) mesh.MarkDynamic();

            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.normals = normals;
            mesh.uv = uv;

            if (!isDynamic) mesh.Optimize();
            _meshFilter.sharedMesh = mesh;

            return mesh;
        }

        public Mesh Render(Vector3[] vertices,
            int[] triangles,
            Vector3[] normals,
            Vector2[] mainTextureUv,
            Vector2[] secondaryTextureUv,
            Material material,
            bool isDynamic)
        {
            Dispose();

            _meshRenderer = gameObject.AddComponent<MeshRenderer>();
            _meshRenderer.sharedMaterial = material;

            _meshFilter = gameObject.AddComponent<MeshFilter>();
            var mesh = new Mesh();
            if (isDynamic) mesh.MarkDynamic();

            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.normals = normals;
            mesh.uv = mainTextureUv;
            mesh.uv2 = secondaryTextureUv;

            if (!isDynamic) mesh.Optimize();
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

        public void RecalculateBoundsNormalsAndTangents()
        {
            if (_meshFilter == null) return;

            var mesh = _meshFilter.sharedMesh;

            // https://docs.unity3d.com/ScriptReference/Mesh.RecalculateBounds.html
            // After modifying vertices you should call this function to ensure the
            // bounding volume is correct. Assigning triangles automatically
            // recalculates the bounding volume.
            mesh.RecalculateBounds();
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
        }

        public Bounds GetRendererBounds()
        {
            return _meshRenderer.bounds;
        }

        public Bounds GetMeshBounds()
        {
            return _meshFilter.sharedMesh.bounds;
        }

        private Shader GetShader(string shaderName)
        {
            if (Shader.Find(shaderName) is {} shader)
            {
                return shader;
            }

            throw new ArgumentException($"Shader: {shaderName} not found.");
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
                Destroy(_meshRenderer.sharedMaterial);
                Destroy(_meshRenderer);
            }
        }
    }
}