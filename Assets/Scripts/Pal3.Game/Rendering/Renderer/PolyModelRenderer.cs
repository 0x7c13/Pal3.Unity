// ------------------------------------------------------------------------
//  Copyright (c) 2021-2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Game.Rendering.Renderer
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using Core.DataReader.Pol;
    using Core.Primitives;
    using Dev.Presenters;
    using Engine.Core.Abstraction;
    using Engine.Core.Implementation;
    using Engine.Coroutine;
    using Engine.Extensions;
    using Engine.Logging;
    using Engine.Renderer;
    using Engine.Services;
    using Material;
    using UnityEngine;
    using Color = Core.Primitives.Color;

    /// <summary>
    /// Poly(.pol) model renderer
    /// </summary>
    public sealed class PolyModelRenderer : GameEntityScript, IDisposable
    {
        private const string ANIMATED_WATER_TEXTURE_DEFAULT_NAME_PREFIX = "w00";
        private const string ANIMATED_WATER_TEXTURE_DEFAULT_NAME = "w0001";
        private const string ANIMATED_WATER_TEXTURE_DEFAULT_EXTENSION = ".dds";
        private const int ANIMATED_WATER_ANIMATION_FRAMES = 30;
        private const float ANIMATED_WATER_ANIMATION_FPS = 20f;

        private ITextureResourceProvider _textureProvider;
        private ITextureFactory _textureFactory;
        private IMaterialManager _materialManager;
        private Dictionary<string, ITexture2D> _textureCache;
        private ITexture2D _whiteTexture;
        private readonly List<StaticMeshRenderer> _meshRenderers = new();
        private readonly List<IGameEntity> _meshEntities = new();
        private float _opacity = 1f;
        // Materials share one tint and texture provider within this POL instance.
        private readonly Dictionary<(string main, string lightmap, GameBoxBlendFlag blend, int nativeMaterial), RenderModelOpacity> _meshMaterials = new();
        private readonly Dictionary<(Color diffuse, Color ambient, bool vertexColors), int> _nativeMaterialIds = new();

        private bool _isStaticObject;
        private Color _tintColor;
        private bool _forceOpaqueWater;
        private PolModelMode _modelMode;
        private List<(RenderModelOpacity materials, ITexture2D firstFrame)> _waterSurfaces;
        private UnityEngine.Coroutine _waterAnimation;

        protected override void OnDisableGameEntity()
        {
            Dispose();
        }

        public void Render(PolFile polFile,
            ITextureResourceProvider textureProvider,
            IMaterialManager materialManager,
            bool isStaticObject,
            Color? tintColor = default,
            bool forceOpaqueWater = false,
            ITextureFactory textureFactory = null,
            bool animateWater = true,
            PolModelMode modelMode = PolModelMode.Game,
            Func<int, int, PolSurfaceAppearance?> surfaceAppearance = null)
        {
            Dispose();
            _textureFactory = textureFactory ?? ServiceLocator.Instance.Get<ITextureFactory>();
            _textureProvider = textureProvider;
            _materialManager = materialManager;
            _isStaticObject = isStaticObject;
            _tintColor = tintColor ?? Color.White;
            _forceOpaqueWater = forceOpaqueWater;
            _modelMode = modelMode;
            _textureCache = BuildTextureCache(polFile, textureProvider);

            var batches = new Dictionary<(RenderModelOpacity materials, Vector3Int cell, PolSurfaceAppearance? appearance), PolMeshBatch>();
            for (int i = 0; i < polFile.Meshes.Length; i++)
            {
                RenderMeshInternal(polFile.NodeDescriptions[i], polFile.Meshes[i], batches, i, surfaceAppearance);
            }
            foreach (var entry in batches)
                RenderGeometry("POL " + entry.Key.cell, entry.Value, entry.Key.materials, appearance: entry.Key.appearance);

            if (animateWater && _waterSurfaces != null)
            {
                _waterAnimation = StartCoroutine(AnimateWaterTextures());
            }
        }

        public void SetOpacity(float opacity)
        {
            _opacity = RenderModelOpacity.Normalize(opacity);
            foreach (RenderModelOpacity materials in _meshMaterials.Values)
                materials.SetOpacity(_opacity);
            if (_waterSurfaces != null)
                foreach (var (materials, _) in _waterSurfaces)
                    materials.SetOpacity(_opacity);
        }

        public Bounds GetRendererBounds()
        {
            StaticMeshRenderer[] renderers = GameEntity.GetComponentsInChildren<StaticMeshRenderer>();
            if (renderers.Length == 0)
            {
                return new Bounds(Transform.Position, Vector3.one);
            }
            Bounds bounds = renderers[0].GetRendererBounds();
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].GetRendererBounds());
            }
            return bounds;
        }

        public Bounds GetMeshBounds()
        {
            StaticMeshRenderer[] renderers = GameEntity.GetComponentsInChildren<StaticMeshRenderer>();
            if (renderers.Length == 0)
            {
                return new Bounds(Vector3.zero, Vector3.one);
            }
            Bounds bounds = renderers[0].GetMeshBounds();
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].GetMeshBounds());
            }
            return bounds;
        }

        private Dictionary<string, ITexture2D> BuildTextureCache(PolFile polFile,
            ITextureResourceProvider textureProvider)
        {
            Dictionary<string, ITexture2D> textureCache = new();
            foreach (PolMesh mesh in polFile.Meshes)
            {
                foreach (PolTexture texture in mesh.Textures)
                {
                    foreach (string textureName in texture.Material.TextureFileNames)
                    {
                        if (string.IsNullOrEmpty(textureName))
                            continue;
                        if (textureCache.ContainsKey(textureName))
                            continue;

                        textureCache[textureName] = textureProvider.GetTexture(textureName);
                    }
                }
            }
            return textureCache;
        }

        private void RenderMeshInternal(PolGeometryNode meshNode, PolMesh mesh,
            Dictionary<(RenderModelOpacity materials, Vector3Int cell, PolSurfaceAppearance? appearance), PolMeshBatch> batches,
            int nodeIndex, Func<int, int, PolSurfaceAppearance?> surfaceAppearance)
        {
            Vector3[] positions = mesh.VertexInfo.GameBoxPositions.ToUnityPositions();
            Vector3[] normals = _materialManager.RequiresNormals ? mesh.VertexInfo.GameBoxNormals.ToUnityNormals() : null;
            UnityEngine.Color32[] colors = (_modelMode == PolModelMode.Authored || _materialManager.NativeLighting != null || surfaceAppearance != null) && (mesh.VertexFvfFlag & GameBoxVertexType.Diffuse) != 0
                ? mesh.VertexInfo.DiffuseColors.ToUnityColor32s() : null;
            Bounds? sourceBounds = null;
            if (_modelMode == PolModelMode.Authored && positions.Length > 0)
            {
                var bounds = new Bounds(positions[0], Vector3.zero);
                foreach (Vector3 position in positions)
                    bounds.Encapsulate(position);
                sourceBounds = bounds;
            }
            var uvs = new Vector2[mesh.VertexInfo.Uvs.Length][];

            Vector2[] GetUvs(int index) => uvs[index] ??= mesh.VertexInfo.Uvs[index].ToUnityVector2s();

            for (int i = 0; i < mesh.Textures.Length; i++)
            {
                List<(string name, ITexture2D texture)> textures = new();
                foreach (string textureName in mesh.Textures[i].Material.TextureFileNames)
                {
                    if (string.IsNullOrEmpty(textureName))
                    {
                        textures.Add((string.Empty, _whiteTexture ??= _textureFactory.CreateWhiteTexture()));
                        continue;
                    }

                    if (_textureCache.TryGetValue(textureName, out ITexture2D textureInCache))
                    {
                        textures.Add((textureName, textureInCache));
                    }
                }

                if (textures.Count == 0)
                {
                    EngineLogger.LogWarning($"0 texture found for {meshNode.Name}");
                    return;
                }

                PolSurfaceAppearance? appearance = surfaceAppearance?.Invoke(nodeIndex, i);
                appearance?.Validate();
                GameBoxBlendFlag blendFlag = mesh.Textures[i].BlendFlag;
                int mainTextureIndex = textures.Count == 1 ? 0 : 1;
                if (_modelMode == PolModelMode.Authored && mainTextureIndex == 1 && string.IsNullOrEmpty(textures[1].name))
                    mainTextureIndex = 0;
                int lightmapIndex = mainTextureIndex == 0 ? -1 : 0;
                var mainTexture = textures[mainTextureIndex];
                var bakedLightmap = lightmapIndex >= 0 ? textures[lightmapIndex] : default;

                RenderModelOpacity materials;
                if (_modelMode == PolModelMode.Game && _materialManager.NativeLighting == null && appearance?.UnlitBackground != true &&
                    mainTexture.name.StartsWith(ANIMATED_WATER_TEXTURE_DEFAULT_NAME, StringComparison.OrdinalIgnoreCase))
                {
                    // Water mutates its main texture, so it owns a separate material.
                    materials = new RenderModelOpacity(_materialManager, RendererType.Pol, mainTexture, bakedLightmap,
                        _tintColor, GameBoxBlendFlag.Opaque,
                        waterAlpha: _forceOpaqueWater ? null : mainTexture.texture.GetPixel(0, 0).a);
                    materials.SetOpacity(_opacity);
                    (_waterSurfaces ??= new()).Add((materials, mainTexture.texture));
                }
                else
                {
                    var key = (mainTexture.name, bakedLightmap.name, blendFlag, NativeMaterialId(mesh.Textures[i].Material, colors != null, appearance?.UnlitBackground == true));
                    if (!_meshMaterials.TryGetValue(key, out materials))
                    {
                        materials = new RenderModelOpacity(_materialManager, RendererType.Pol, mainTexture, bakedLightmap, _tintColor, blendFlag,
                            sourceMaterial: mesh.Textures[i].Material);
                        materials.SetOpacity(_opacity);
                        _meshMaterials.Add(key, materials);
                    }
                }

                var map = new PolMeshIndexMap(positions.Length, mesh.Textures[i].GameBoxTriangles);
                if (map.SourceIndices.Length == 0)
                    continue;
                bool sharedLighting = _materialManager.NativeLighting == null || bakedLightmap.texture != null || colors != null;
                bool batchable = _modelMode == PolModelMode.Game && sharedLighting && _isStaticObject && blendFlag == GameBoxBlendFlag.Opaque &&
                    !mainTexture.name.StartsWith(ANIMATED_WATER_TEXTURE_DEFAULT_NAME, StringComparison.OrdinalIgnoreCase);
                Vector3Int cell = default;
                batchable &= PolMeshBatch.TryGetCell(map, positions, out cell);
                PolMeshBatch geometry;
                if (batchable)
                {
                    var key = (materials, cell, appearance);
                    if (batches.TryGetValue(key, out geometry) &&
                        geometry.Positions.Count + map.SourceIndices.Length > PolMeshBatch.MaximumVertices)
                    {
                        RenderGeometry("POL " + cell, geometry, materials, appearance: appearance);
                        batches.Remove(key);
                        geometry = null;
                    }
                    if (geometry == null)
                        batches.Add(key, geometry = new PolMeshBatch());
                }
                else
                    geometry = new PolMeshBatch();

#if UNITY_EDITOR
                geometry.Sources.Add(new PolMeshBatchInfoPresenter.SourceGroup
                {
                    nodeName = meshNode.Name,
                    materialGroupIndex = i,
                    blendFlag = blendFlag,
                    material = mesh.Textures[i].Material,
                    vertexStart = geometry.Positions.Count,
                    vertexCount = map.SourceIndices.Length,
                });
#endif
                geometry.Add(map, positions, normals, GetUvs(mainTextureIndex), GetUvs(Math.Max(lightmapIndex, 0)), colors, geometricNormals: appearance?.GeometricNormals == true);
                if (!batchable)
                {
                    IGameEntity entity = RenderGeometry(meshNode.Name, geometry, materials, sourceBounds, appearance);
#if UNITY_EDITOR
                    MaterialInfoPresenter presenter = entity.AddComponent<MaterialInfoPresenter>();
                    presenter.blendFlag = blendFlag;
                    presenter.material = mesh.Textures[i].Material;
#endif
                }
            }
        }

        private int NativeMaterialId(GameBoxMaterial material, bool vertexColors, bool painted)
        {
            if (_materialManager.NativeLighting == null && !painted)
                return vertexColors ? -1 : 0;
            var key = (material.Diffuse, material.Ambient, vertexColors);
            if (!_nativeMaterialIds.TryGetValue(key, out int id))
                _nativeMaterialIds.Add(key, id = _nativeMaterialIds.Count + 1);
            return id;
        }

        private IGameEntity RenderGeometry(string name, PolMeshBatch geometry, RenderModelOpacity materials, Bounds? sourceBounds = null, PolSurfaceAppearance? appearance = null)
        {
            IGameEntity entity = GameEntityFactory.Create(name, GameEntity, worldPositionStays: false);
            entity.IsStatic = _isStaticObject;
            StaticMeshRenderer renderer = entity.AddComponent<StaticMeshRenderer>();
            _meshRenderers.Add(renderer);
            _meshEntities.Add(entity);
            Mesh mesh = renderer.Render(geometry.Positions.ToArray(), geometry.Triangles.ToArray(),
                geometry.Normals.Count > 0 ? geometry.Normals.ToArray() : null,
                (0, geometry.MainUvs.ToArray()), (1, geometry.LightmapUvs.ToArray()), materials.Materials, isDynamic: false);
            if (geometry.Colors.Count > 0)
                mesh.SetColors(geometry.Colors);
            if (sourceBounds.HasValue)
                mesh.bounds = sourceBounds.Value;
            materials.Bind(renderer, apply: false, vertexColors: geometry.Colors.Count > 0, environment: _isStaticObject);
            if (appearance?.UnlitBackground == true)
            {
                // Authored painted backgrounds remain independent of scene lights.
                // A property block keeps this choice local even when materials are pooled.
                var meshRenderer = renderer.GetComponent<MeshRenderer>();
                var properties = new MaterialPropertyBlock();
                meshRenderer.GetPropertyBlock(properties);
                properties.SetFloat(Shader.PropertyToID("_UnlitBackground"), 1);
                properties.SetFloat(Shader.PropertyToID("_BackgroundBrightness"), appearance.Value.Brightness);
                // Painted POL layers use the original texture-stage gain even
                // when a water texture selected a different material template.
                properties.SetFloat(Shader.PropertyToID("_LightmapScale"), GameMaterialManager.PolLightmapScale);
                meshRenderer.SetPropertyBlock(properties);
                meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                meshRenderer.receiveShadows = false;
            }
#if UNITY_EDITOR
            PolMeshBatchInfoPresenter presenter = entity.AddComponent<PolMeshBatchInfoPresenter>();
            presenter.sources = geometry.Sources.ToArray();
#endif
            return entity;
        }

        private IEnumerator AnimateWaterTextures()
        {
            var frames = new ITexture2D[ANIMATED_WATER_ANIMATION_FRAMES];
            for (int i = 1; i < frames.Length; i++)
            {
                string name = ANIMATED_WATER_TEXTURE_DEFAULT_NAME_PREFIX +
                    $"{i + 1:00}" + ANIMATED_WATER_TEXTURE_DEFAULT_EXTENSION;
                frames[i] = _textureCache.TryGetValue(name, out ITexture2D texture)
                    ? texture
                    : _textureProvider.GetTexture(name);
            }

            object delay = CoroutineYieldInstruction.WaitForSeconds(1 / ANIMATED_WATER_ANIMATION_FPS);
            while (true)
            {
                foreach (var (materials, firstFrame) in _waterSurfaces)
                {
                    materials.ChangeTexture(ANIMATED_WATER_TEXTURE_DEFAULT_NAME, firstFrame);
                }
                yield return delay;

                for (int i = 1; i < frames.Length; i++)
                {
                    ITexture2D frame = frames[i];
                    if (frame != null)
                    {
                        foreach (var (materials, _) in _waterSurfaces)
                        {
                            materials.ChangeTexture(ANIMATED_WATER_TEXTURE_DEFAULT_NAME, frame);
                        }
                    }

                    yield return delay;
                }
            }
        }

        public void Dispose()
        {
            if (_waterAnimation != null)
            {
                StopCoroutine(_waterAnimation);
                _waterAnimation = null;
            }

            foreach (StaticMeshRenderer meshRenderer in _meshRenderers)
            {
                if (meshRenderer == null)
                    continue;
                meshRenderer.Dispose();
                meshRenderer.Destroy();
            }
            _meshRenderers.Clear();
            foreach (IGameEntity entity in _meshEntities)
                entity.Destroy();
            _meshEntities.Clear();

            foreach (RenderModelOpacity materials in _meshMaterials.Values)
            {
                materials.Dispose();
            }
            _meshMaterials.Clear();
            _nativeMaterialIds.Clear();

            if (_waterSurfaces != null)
            {
                foreach (var (materials, _) in _waterSurfaces)
                {
                    materials.Dispose();
                }
                _waterSurfaces = null;
            }
            // The texture factory exposes Unity's shared built-in white texture.
            _whiteTexture = null;
            _textureCache = null;
        }
    }
}
