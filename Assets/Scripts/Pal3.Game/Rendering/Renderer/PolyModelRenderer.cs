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
        private Dictionary<string, ITexture2D> _textureCache = new ();

        private bool _isStaticObject;
        private Color _tintColor;
        private bool _isWaterSurfaceOpaque;
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
            bool isWaterSurfaceOpaque = default)
        {
            _textureProvider = textureProvider;
            _materialManager = materialManager;
            _isStaticObject = isStaticObject;
            _tintColor = tintColor ?? Color.White;
            _isWaterSurfaceOpaque = isWaterSurfaceOpaque;
            _textureCache = BuildTextureCache(polFile, textureProvider);

            for (int i = 0; i < polFile.Meshes.Length; i++)
            {
                RenderMeshInternal(polFile.NodeDescriptions[i], polFile.Meshes[i]);
            }

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

                        ITexture2D texture2D;

                        if (_materialManager.ShaderType == MaterialShaderType.Lit)
                        {
                            // No need to load pre-baked shadow texture if
                            // material is lit material, since shadow texture
                            // will be generated by shader in runtime.
                            // Note: all shadow texture name starts with "^"
                            texture2D = textureName.StartsWith("^") ?
                                null : textureProvider.GetTexture(textureName);
                        }
                        else
                        {
                            texture2D = textureProvider.GetTexture(textureName);
                        }

                        textureCache[textureName] = texture2D;
                    }
                }
            }
            return textureCache;
        }

        private void RenderMeshInternal(PolGeometryNode meshNode, PolMesh mesh)
        {
            Vector3[] positions = mesh.VertexInfo.GameBoxPositions.ToUnityPositions();
            Vector3[] normals = mesh.VertexInfo.GameBoxNormals.ToUnityNormals();
            var uvs = new Vector2[mesh.VertexInfo.Uvs.Length][];

            Vector2[] GetUvs(int index) => uvs[index] ??= mesh.VertexInfo.Uvs[index].ToUnityVector2s();

            for (int i = 0; i < mesh.Textures.Length; i++)
            {
                List<(string name, ITexture2D texture)> textures = new();
                foreach (string textureName in mesh.Textures[i].Material.TextureFileNames)
                {
                    if (string.IsNullOrEmpty(textureName))
                    {
                        textures.Add((textureName, _textureFactory.CreateWhiteTexture()));
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

                IGameEntity meshEntity = GameEntityFactory.Create(meshNode.Name, GameEntity, worldPositionStays: false);
                meshEntity.IsStatic = _isStaticObject;

                // Attach BlendFlag and GameBoxMaterial to the GameEntity for better debuggability
                #if UNITY_EDITOR
                MaterialInfoPresenter materialInfoPresenter = meshEntity.AddComponent<MaterialInfoPresenter>();
                materialInfoPresenter.blendFlag = mesh.Textures[i].BlendFlag;
                materialInfoPresenter.material = mesh.Textures[i].Material;
                #endif

                StaticMeshRenderer meshRenderer = meshEntity.AddComponent<StaticMeshRenderer>();
                GameBoxBlendFlag blendFlag = mesh.Textures[i].BlendFlag;

                IMaterial[] CreateMaterials(bool isWaterSurface, int mainTextureIndex, int shadowTextureIndex = -1)
                {
                    IMaterial[] materials;
                    float waterSurfaceOpacity = 1.0f;

                    if (isWaterSurface)
                    {
                        materials = new IMaterial[1];

                        if (!_isWaterSurfaceOpaque)
                        {
                            waterSurfaceOpacity = textures[mainTextureIndex].texture.GetPixel(0, 0).a;
                        }
                        else
                        {
                            blendFlag = GameBoxBlendFlag.Opaque;
                        }

                        materials[0] = _materialManager.CreateWaterMaterial(
                            mainTexture: textures[mainTextureIndex],
                            shadowTexture: shadowTextureIndex >= 0 ? textures[shadowTextureIndex] : default,
                            opacity: waterSurfaceOpacity,
                            blendFlag);
                    }
                    else
                    {
                        materials = _materialManager.CreateStandardMaterials(
                            RendererType.Pol,
                            mainTexture: textures[mainTextureIndex],
                            shadowTexture: shadowTextureIndex >= 0 ? textures[shadowTextureIndex] : default,
                            tintColor: _tintColor,
                            blendFlag);
                    }
                    return materials;
                }

                if (textures.Count >= 1)
                {
                    int mainTextureIndex = textures.Count == 1 ? 0 : 1;
                    int shadowTextureIndex = textures.Count == 1 ? -1 : 0;

                    bool isWaterSurface = textures[mainTextureIndex].name
                        .StartsWith(ANIMATED_WATER_TEXTURE_DEFAULT_NAME, StringComparison.OrdinalIgnoreCase);

                    IMaterial[] materials = CreateMaterials(isWaterSurface, mainTextureIndex, shadowTextureIndex);

                    if (isWaterSurface)
                    {
                        (_waterSurfaces ??= new()).Add((materials, textures[mainTextureIndex].texture));
                    }

                    _ = meshRenderer.Render(
                        vertices: positions,
                        triangles: mesh.Textures[i].GameBoxTriangles.ToUnityTriangles(),
                        normals: normals,
                        mainTextureUvs: (channel: 0, uvs: GetUvs(mainTextureIndex)),
                        secondaryTextureUvs: (channel: 1, uvs: GetUvs(Math.Max(shadowTextureIndex, 0))),
                        materials: materials,
                        isDynamic: false);
                }
            }
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
            _waterSurfaces = null;

            foreach (StaticMeshRenderer meshRenderer in GameEntity.GetComponentsInChildren<StaticMeshRenderer>())
            {
                _materialManager.ReturnToPool(meshRenderer.GetMaterials());
                meshRenderer.Dispose();
                meshRenderer.Destroy();
            }
        }
    }
}