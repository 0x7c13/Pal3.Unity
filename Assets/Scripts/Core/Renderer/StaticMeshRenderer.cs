// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE.txt in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Core.Renderer
{
    using System;
    using UnityEngine;

    public class StaticMeshRenderer : MonoBehaviour
    {
        private MeshRenderer _meshRenderer;
        private MeshFilter _meshFilter;

        public void Render(Vector3[] vertices, int[] triangles, string shaderName)
        {
            Dispose();

            _meshRenderer = gameObject.AddComponent<MeshRenderer>();
            _meshFilter = gameObject.AddComponent<MeshFilter>();

            var shader = GetShader(shaderName);

            var material = new Material(shader);
            _meshRenderer.sharedMaterial = material;

            var mesh = new Mesh
            {
                vertices = vertices,
                triangles = triangles,
            };

            _meshFilter.sharedMesh = mesh;
        }

        public void Render(Vector3[] vertices,
            int[] triangles,
            Vector3[] normals,
            Vector2[] uv,
            Material material)
        {
            Dispose();

            _meshRenderer = gameObject.AddComponent<MeshRenderer>();
            _meshRenderer.sharedMaterial = material;
            _meshFilter = gameObject.AddComponent<MeshFilter>();

            var mesh = new Mesh
            {
                vertices = vertices,
                triangles = triangles,
                normals = normals,
                uv = uv
            };

            _meshFilter.sharedMesh = mesh;

            if (normals.Length == 0)
            {
                // _meshFilter.sharedMesh.RecalculateNormals();
                // _meshFilter.sharedMesh.RecalculateTangents();
            }
        }

        public void Render(Vector3[] vertices,
            int[] triangles,
            Vector3[] normals,
            Vector2[] mainTextureUv,
            Vector2[] secondaryTextureUv,
            Material material)
        {
            Dispose();

            _meshRenderer = gameObject.AddComponent<MeshRenderer>();
            _meshRenderer.sharedMaterial = material;

            _meshFilter = gameObject.AddComponent<MeshFilter>();
            var mesh = new Mesh
            {
                vertices = vertices,
                triangles = triangles,
                normals = normals,
                uv = mainTextureUv,
                uv2 = secondaryTextureUv,
            };

            _meshFilter.sharedMesh = mesh;

            if (normals.Length == 0)
            {
                // _meshFilter.sharedMesh.RecalculateNormals();
                // _meshFilter.sharedMesh.RecalculateTangents();
            }
        }

        public void UpdateMesh(
            Vector3[] vertices,
            int[] triangles,
            Vector3[] normals,
            Vector2[] uv)
        {
            if (_meshFilter == null) return;

            var mesh = _meshFilter.sharedMesh;
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.normals = normals;
            mesh.uv = uv;
        }

        public void ApplyMatrix(Matrix4x4 matrix)
        {
            var mesh = _meshFilter.sharedMesh;

            var newVerts = new Vector3[mesh.vertices.Length];

            for (var j = 0; j < mesh.vertices.Length; j++)
            {
                newVerts[j] = matrix.MultiplyPoint3x4(mesh.vertices[j]);
            }

            mesh.vertices = newVerts;

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