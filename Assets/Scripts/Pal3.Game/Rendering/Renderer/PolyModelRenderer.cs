// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2025, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

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
        // Materials share one tint and texture provider within this POL instance.
        private readonly Dictionary<(string main, string lightmap, GameBoxBlendFlag blend), IMaterial[]> _meshMaterials = new();

        private bool _isStaticObject;
        private Color _tintColor;
        private bool _forceOpaqueWater;
        private List<(IMaterial[] materials, ITexture2D firstFrame)> _waterSurfaces;
        private UnityEngine.Coroutine _waterAnimation;

        protected override void OnEnableGameEntity()
        {
            _textureFactory = ServiceLocator.Instance.Get<ITextureFactory>();
        }

        protected override void OnDisableGameEntity()
        {
            Dispose();
        }

        public void Render(PolFile polFile,
            ITextureResourceProvider textureProvider,
            IMaterialManager materialManager,
            bool isStaticObject,
            Color? tintColor = default,
            bool forceOpaqueWater = false)
        {
            _textureProvider = textureProvider;
            _materialManager = materialManager;
            _isStaticObject = isStaticObject;
            _tintColor = tintColor ?? Color.White;
            _forceOpaqueWater = forceOpaqueWater;
            _textureCache = BuildTextureCache(polFile, textureProvider);

            var batches = new Dictionary<(IMaterial[] materials, Vector3Int cell), PolMeshBatch>();
            for (int i = 0; i < polFile.Meshes.Length; i++)
            {
                RenderMeshInternal(polFile.NodeDescriptions[i], polFile.Meshes[i], batches);
            }
            foreach (var entry in batches)
                RenderGeometry("POL " + entry.Key.cell, entry.Value, entry.Key.materials);

            if (_waterSurfaces != null)
            {
                _waterAnimation = StartCoroutine(AnimateWaterTextures());
            }
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
                        if (string.IsNullOrEmpty(textureName)) continue;
                        if (textureCache.ContainsKey(textureName)) continue;

                        textureCache[textureName] = textureProvider.GetTexture(textureName);
                    }
                }
            }
            return textureCache;
        }

        private void RenderMeshInternal(PolGeometryNode meshNode, PolMesh mesh,
            Dictionary<(IMaterial[] materials, Vector3Int cell), PolMeshBatch> batches)
        {
            Vector3[] positions = mesh.VertexInfo.GameBoxPositions.ToUnityPositions();
            Vector3[] normals = _materialManager.UsesToonRendering ? mesh.VertexInfo.GameBoxNormals.ToUnityNormals() : null;
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

                GameBoxBlendFlag blendFlag = mesh.Textures[i].BlendFlag;
                int mainTextureIndex = textures.Count == 1 ? 0 : 1;
                int lightmapIndex = textures.Count == 1 ? -1 : 0;
                var mainTexture = textures[mainTextureIndex];
                var bakedLightmap = lightmapIndex >= 0 ? textures[lightmapIndex] : default;

                IMaterial[] materials;
                if (mainTexture.name.StartsWith(ANIMATED_WATER_TEXTURE_DEFAULT_NAME, StringComparison.OrdinalIgnoreCase))
                {
                    // Water mutates its main texture, so it owns a separate material.
                    materials = _forceOpaqueWater
                        ? _materialManager.CreateStandardMaterials(
                            RendererType.Pol, mainTexture, bakedLightmap, _tintColor, GameBoxBlendFlag.Opaque)
                        : new[]
                        {
                            _materialManager.CreateWaterMaterial(
                                mainTexture, bakedLightmap, mainTexture.texture.GetPixel(0, 0).a),
                        };
                    (_waterSurfaces ??= new()).Add((materials, mainTexture.texture));
                }
                else
                {
                    var key = (mainTexture.name, bakedLightmap.name, blendFlag);
                    if (!_meshMaterials.TryGetValue(key, out materials))
                    {
                        materials = _materialManager.CreateStandardMaterials(
                            RendererType.Pol, mainTexture, bakedLightmap, _tintColor, blendFlag);
                        _meshMaterials.Add(key, materials);
                    }
                }

                var map = new PolMeshIndexMap(positions.Length, mesh.Textures[i].GameBoxTriangles);
                if (map.SourceIndices.Length == 0) continue;
                bool batchable = _isStaticObject && blendFlag == GameBoxBlendFlag.Opaque &&
                    !mainTexture.name.StartsWith(ANIMATED_WATER_TEXTURE_DEFAULT_NAME, StringComparison.OrdinalIgnoreCase);
                Vector3Int cell = default;
                batchable &= PolMeshBatch.TryGetCell(map, positions, out cell);
                PolMeshBatch geometry;
                if (batchable)
                {
                    var key = (materials, cell);
                    if (batches.TryGetValue(key, out geometry) &&
                        geometry.Positions.Count + map.SourceIndices.Length > PolMeshBatch.MaximumVertices)
                    {
                        RenderGeometry("POL " + cell, geometry, materials);
                        batches.Remove(key);
                        geometry = null;
                    }
                    if (geometry == null) batches.Add(key, geometry = new PolMeshBatch());
                }
                else geometry = new PolMeshBatch();

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
                geometry.Add(map, positions, normals, GetUvs(mainTextureIndex), GetUvs(Math.Max(lightmapIndex, 0)));
                if (!batchable)
                {
                    IGameEntity entity = RenderGeometry(meshNode.Name, geometry, materials);
                    #if UNITY_EDITOR
                    MaterialInfoPresenter presenter = entity.AddComponent<MaterialInfoPresenter>();
                    presenter.blendFlag = blendFlag;
                    presenter.material = mesh.Textures[i].Material;
                    #endif
                }
            }
        }

        private IGameEntity RenderGeometry(string name, PolMeshBatch geometry, IMaterial[] materials)
        {
            IGameEntity entity = GameEntityFactory.Create(name, GameEntity, worldPositionStays: false);
            entity.IsStatic = _isStaticObject;
            StaticMeshRenderer renderer = entity.AddComponent<StaticMeshRenderer>();
            _meshRenderers.Add(renderer);
            renderer.Render(geometry.Positions.ToArray(), geometry.Triangles.ToArray(),
                geometry.Normals.Count > 0 ? geometry.Normals.ToArray() : null,
                (0, geometry.MainUvs.ToArray()), (1, geometry.LightmapUvs.ToArray()), materials, isDynamic: false);
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
                    materials[0].SetMainTexture(firstFrame);
                }
                yield return delay;

                for (int i = 1; i < frames.Length; i++)
                {
                    ITexture2D frame = frames[i];
                    if (frame != null)
                    {
                        foreach (var (materials, _) in _waterSurfaces)
                        {
                            materials[0].SetMainTexture(frame);
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
                if (meshRenderer == null) continue;
                meshRenderer.Dispose();
                meshRenderer.Destroy();
            }
            _meshRenderers.Clear();

            foreach (IMaterial[] materials in _meshMaterials.Values)
            {
                _materialManager.ReturnToPool(materials);
            }
            _meshMaterials.Clear();

            if (_waterSurfaces != null)
            {
                foreach (var (materials, _) in _waterSurfaces)
                {
                    _materialManager.ReturnToPool(materials);
                }
                _waterSurfaces = null;
            }
        }
    }
}