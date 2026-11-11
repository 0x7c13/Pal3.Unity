// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2025, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Engine.Renderer
{
    using System;
    using System.Linq;
    using Core.Abstraction;
    using Core.Implementation;
    using Extensions;
    using UnityEngine;
    using UnityEngine.Rendering;

    public sealed class StaticMeshRenderer : GameEntityScript, IDisposable
    {
        private MeshRenderer _meshRenderer;
        private MeshFilter _meshFilter;
        private IMaterial[] _materials;

        protected override void OnDisableGameEntity()
        {
            Dispose();
        }

        public Mesh Render(Vector3[] vertices,
            int[] triangles,
            Vector3[] normals,
            (int channel, Vector2[] uvs) mainTextureUvs,
            (int channel, Vector2[] uvs) secondaryTextureUvs,
            IMaterial[] materials,
            bool isDynamic)
        {
            Dispose();

            _meshRenderer = GameEntity.AddComponent<MeshRenderer>();
            SetMaterials(materials);

            _meshFilter = GameEntity.AddComponent<MeshFilter>();

            Mesh mesh = new();

            if (isDynamic)
            {
                mesh.MarkDynamic();
            }

            mesh.SetVertices(vertices);

            if (triangles != null)
            {
                mesh.SetTriangles(triangles, 0);
            }

            if (normals != null) // normals can be null
            {
                mesh.SetNormals(normals);
            }

            if (mainTextureUvs != default)
            {
                mesh.SetUVs(mainTextureUvs.channel, mainTextureUvs.uvs);
            }

            if (secondaryTextureUvs != default)
            {
                mesh.SetUVs(secondaryTextureUvs.channel, secondaryTextureUvs.uvs);
            }

            if (triangles == null)
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

        private static readonly int ReceiveShadowsId = Shader.PropertyToID("_ReceiveShadows");
        private bool _shadowsDisabled;

        public void SetMaterials(IMaterial[] materials)
        {
            _meshRenderer.sharedMaterials = materials.Select(material => material.NativeObject as Material).ToArray();
            _materials = materials;
            if (_shadowsDisabled) DisableShadows();
        }

        public void DisableShadows()
        {
            _shadowsDisabled = true;
            if (_meshRenderer == null) return;

            _meshRenderer.receiveShadows = false;
            _meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
            foreach (IMaterial material in _materials)
            {
                if (material.NativeObject is Material unityMaterial && unityMaterial.HasProperty(ReceiveShadowsId))
                {
                    unityMaterial.SetFloat(ReceiveShadowsId, 0f);
                }
            }
        }

        public Mesh GetMesh()
        {
            return _meshFilter == null ? null : _meshFilter.sharedMesh;
        }

        public bool IsVisible => _meshRenderer != null && _meshRenderer.isVisible;

        public Bounds GetRendererBounds()
        {
            return _meshRenderer.bounds;
        }

        public Bounds GetMeshBounds()
        {
            return _meshFilter.sharedMesh.bounds;
        }

        public void Dispose()
        {
            if (_meshFilter != null)
            {
                _meshFilter.sharedMesh.Destroy();
                _meshFilter.Destroy();
                _meshFilter = null;
            }

            if (_meshRenderer != null)
            {
                _meshRenderer.enabled = false;
                _meshRenderer.Destroy();
                _meshRenderer = null;
            }

            // Do not destroy materials here, because their lifetime
            // is managed by the IMaterialFactory.
            _materials = null;
        }
    }
}