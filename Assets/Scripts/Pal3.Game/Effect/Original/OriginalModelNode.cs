// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Game.Effect.Original
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Core.DataReader.Pol;
    using Engine.Core.Abstraction;
    using Engine.Extensions;
    using UnityEngine.Rendering;
    using Core.DataReader.Data;
    using Engine.Core.Implementation;
    using Rendering.Material;
    using Rendering.Renderer;
    using UnityEngine;

    internal sealed class OriginalModelNode : OriginalEffectNode
    {
        private const float ReferenceFramesPerSecond = 60f;
        private readonly CvdModelRenderer _model;
        private readonly GameMaterialManager _materials;
        private readonly Renderer[] _renderers;
        private float _animationStart;
        private float _opacity = 1;
        private readonly List<IMaterial[]> _polMaterials = new();
        private readonly List<Mesh> _meshes = new();
        private readonly List<Material> _opacityMaterials = new();
        private readonly MaterialPropertyBlock _properties = new();

        public OriginalModelNode(GameObject root, EffectDefinition d, EffectTask[] tasks, Vector3 origin, float birth, OriginalEffectResources resources)
            : base(root, d, tasks, origin, birth)
        {
            // CEffModel ignores the flexeff tint/additive/depth flags: the model
            // owns those settings. SetBlend(ALPHA) overrides them only for fades.
            _materials = new GameMaterialManager(new UnityMaterialFactory(), false);
            try
            {
                if (d.TextureName.EndsWith(".cvd", StringComparison.OrdinalIgnoreCase))
                {
                    _model = root.AddComponent<CvdModelRenderer>();
                    _model.Init(resources.Model(d.TextureName), resources.ModelTextureProvider(d.TextureName), _materials);
                }
                else if (d.TextureName.EndsWith(".pol", StringComparison.OrdinalIgnoreCase))
                    RenderPol(root, d.TextureName, resources);
                else
                    throw new NotSupportedException($"Unsupported original model {d.TextureName}");
            }
            catch
            {
                _model?.Dispose();
                foreach (Mesh mesh in _meshes)
                    OriginalEffectResources.Destroy(mesh);
                foreach (IMaterial[] materials in _polMaterials)
                    _materials.ReturnToPool(materials);
                _materials.Dispose();
                throw;
            }
            _renderers = root.GetComponentsInChildren<Renderer>();
            foreach (Transform child in root.GetComponentsInChildren<Transform>())
                child.gameObject.layer = root.layer;
        }

        private void RenderPol(GameObject root, string path, OriginalEffectResources resources)
        {
            ITextureResourceProvider provider = resources.ModelTextureProvider(path);
            foreach (PolMesh source in resources.PolModel(path).Meshes)
            {
                Vector3[] positions = source.VertexInfo.GameBoxPositions.ToUnityPositions();
                foreach (PolTexture texture in source.Textures)
                {
                    if (texture.GameBoxTriangles.Length == 0)
                        continue;
                    string[] names = texture.Material.TextureFileNames;
                    int main = names.Length > 1 && !string.IsNullOrEmpty(names[1]) ? 1 : 0;
                    ITexture2D Texture(int index) => string.IsNullOrEmpty(names[index])
                        ? new UnityTexture2D(Texture2D.whiteTexture) : provider.GetTexture(names[index]);
                    IMaterial[] materials = _materials.CreateStandardMaterials(Rendering.RendererType.Pol,
                        (names[main], Texture(main)), main == 1 ? (names[0], Texture(0)) : default,
                        Core.Primitives.Color.White, texture.BlendFlag);
                    _polMaterials.Add(materials);
                    var mesh = new Mesh { name = "Original effect POL", indexFormat = IndexFormat.UInt32, hideFlags = HideFlags.DontSave };
                    _meshes.Add(mesh);
                    mesh.vertices = positions;
                    mesh.uv = source.VertexInfo.Uvs[main].ToUnityVector2s();
                    mesh.uv2 = source.VertexInfo.Uvs[0].ToUnityVector2s();
                    mesh.triangles = texture.GameBoxTriangles.Reverse().ToArray();
                    mesh.RecalculateBounds();
                    var child = new GameObject("Original POL section") { hideFlags = HideFlags.DontSave, layer = root.layer };
                    child.transform.SetParent(root.transform, false);
                    child.AddComponent<MeshFilter>().sharedMesh = mesh;
                    MeshRenderer renderer = child.AddComponent<MeshRenderer>();
                    renderer.sharedMaterials = materials.Select(m => (Material)m.NativeObject).ToArray();
                    renderer.shadowCastingMode = ShadowCastingMode.Off;
                    renderer.receiveShadows = false;
                }
            }
        }

        private void EnableOpacityOverride()
        {
            if (_opacityMaterials.Count > 0)
                return;
            foreach (Renderer renderer in _renderers)
            {
                if (renderer.sharedMaterial == null)
                    continue;
                var material = new Material(renderer.sharedMaterial)
                {
                    shader = Shader.Find("Pal3/Transparent"),
                    hideFlags = HideFlags.DontSave
                };
                material.SetInt("_BlendSrcFactor", (int)BlendMode.SrcAlpha);
                material.SetInt("_BlendDstFactor", (int)BlendMode.OneMinusSrcAlpha);
                material.SetFloat("_Threshold", 2);
                material.renderQueue = (int)RenderQueue.Transparent;
                _opacityMaterials.Add(material);
                // Existing CVD/POL cutout + blend passes become a single alpha pass.
                renderer.sharedMaterials = new[] { material };
            }
        }

        protected override void Apply(EffectTask t, OriginalTaskClock clock, float dt)
        {
            if (clock.StartedNow)
                _animationStart = LocalTime;
            base.Apply(t, clock, dt);
            if (t.SpeedAlpha != 0)
            {
                EnableOpacityOverride();
                _opacity = Mathf.Clamp01(_opacity + t.SpeedAlpha * dt);
            }
        }

        protected override void ApplyRotationAndStretch(EffectTask t, float dt)
        {
            // CEffModel.cpp:238-245 adds these values once per update. Normalize
            // to our chosen 60 FPS compatibility reference, independently of
            // the display frame rate and the editor/seek integration step.
            base.ApplyRotationAndStretch(t, dt * ReferenceFramesPerSecond);
        }

        public override void Render(Camera camera)
        {
            Root.transform.localPosition = UnityPosition(Position + Orbit);
            Root.transform.localRotation = UnityRotation(Rotation);
            Root.transform.localScale = Scale;
            _properties.SetColor("_TintColor", new Color(1, 1, 1, _opacity));
            foreach (Renderer renderer in _renderers)
            {
                renderer.enabled = Alive && Visible;
                if (_opacityMaterials.Count > 0)
                    renderer.SetPropertyBlock(_properties);
            }
            if (Alive && Visible)
                _model?.SetCurrentTime(Mathf.Max(0, LocalTime - _animationStart));
        }

        public override void Dispose()
        {
            _model?.Dispose();
            foreach (Mesh mesh in _meshes)
                OriginalEffectResources.Destroy(mesh);
            foreach (Material material in _opacityMaterials)
                OriginalEffectResources.Destroy(material);
            foreach (IMaterial[] materials in _polMaterials)
                _materials.ReturnToPool(materials);
            _materials.Dispose();
            base.Dispose();
        }
    }
}
