// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Renderer
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using Core.DataLoader;
    using Core.DataReader.Pol;
    using Core.Renderer;
    using Dev;
    using UnityEngine;
    using Debug = UnityEngine.Debug;

    /// <summary>
    /// Poly(.pol) model renderer
    /// </summary>
    public class PolyModelRenderer : MonoBehaviour
    {
        private const int TRANSPARENT_RENDER_QUEUE_INDEX = 3000;

        private const string ANIMATED_WATER_TEXTURE_DEFAULT_NAME_PREFIX = "w00";
        private const string ANIMATED_WATER_TEXTURE_DEFAULT_NAME = "w0001";
        private const string ANIMATED_WATER_TEXTURE_DEFAULT_EXTENSION = ".dds";
        private const int ANIMATED_WATER_ANIMATION_FRAMES = 30;
        private const float ANIMATED_WATER_ANIMATION_FPS = 20f;

        private ITextureResourceProvider _textureProvider;
        private readonly List<Coroutine> _waterAnimations = new ();
        private Dictionary<string, Texture2D> _textureCache = new ();

        private Color _tintColor;

        private readonly int _mainTexturePropertyId = Shader.PropertyToID("_MainTex");
        private readonly int _shadowTexturePropertyId = Shader.PropertyToID("_ShadowTex");
        private readonly int _cutoffPropertyId = Shader.PropertyToID("_Cutoff");
        private readonly int _tintColorPropertyId = Shader.PropertyToID("_TintColor");
        private readonly int _transparencyPropertyId = Shader.PropertyToID("_Transparency");
        private Shader _standardShader;
        private Shader _standardNoShadowShader;
        private readonly Dictionary<string, Material> _materials = new ();
        private bool _disableTransparency;

        public void Render(PolFile polFile, ITextureResourceProvider textureProvider, Color tintColor, bool disableTransparency = false)
        {
            _textureProvider = textureProvider;
            _tintColor = tintColor;
            _textureCache = BuildTextureCache(polFile, textureProvider);
            _disableTransparency = disableTransparency;

            _standardShader = Shader.Find("Pal3/Standard");
            _standardNoShadowShader = Shader.Find("Pal3/StandardNoShadow");

            for (var i = 0; i < polFile.Meshes.Length; i++)
            {
                RenderMeshInternal(
                    polFile.NodeDescriptions[i].Name,
                    polFile.Meshes[i]);
            }
        }

        public Bounds GetRendererBounds()
        {
            var bounds = new Bounds (transform.position, Vector3.one);

            foreach (var meshRenderer in GetComponentsInChildren<StaticMeshRenderer>())
            {
                bounds.Encapsulate(meshRenderer.GetRendererBounds());
            }

            return bounds;
        }

        private Dictionary<string, Texture2D> BuildTextureCache(PolFile polFile,
            ITextureResourceProvider textureProvider)
        {
            Dictionary<string, Texture2D> textureCache = new();
            foreach (var mesh in polFile.Meshes)
            {
                foreach (var texture in mesh.Textures)
                {
                    foreach (var textureName in texture.TextureNames)
                    {
                        if (string.IsNullOrEmpty(textureName)) continue;
                        if (textureCache.ContainsKey(textureName)) continue;
                        var texture2D = textureProvider.GetTexture(textureName);
                        if (texture2D != null) textureCache[textureName] = texture2D;
                    }
                }
            }
            return textureCache;
        }

        private void RenderMeshInternal(string meshName, PolMesh mesh)
        {
            for (var i = 0; i < mesh.Textures.Length; i++)
            {
                var textures = new List<(string name, Texture2D texture)>();
                foreach (var textureName in mesh.Textures[i].TextureNames)
                {
                    if (string.IsNullOrEmpty(textureName)) continue;

                    if (_textureCache.ContainsKey(textureName))
                    {
                        textures.Add((textureName, _textureCache[textureName]));
                    }
                }

                if (textures.Count == 0)
                {
                    Debug.LogWarning($"0 texture found for {meshName}");
                    return;
                }

                var meshObject = new GameObject(meshName);

                // Attach BlendFlag and GameBoxMaterial to the GameObject for better debuggability
                #if UNITY_EDITOR
                var materialInfoPresenter = meshObject.AddComponent<MaterialInfoPresenter>();
                materialInfoPresenter.blendFlag = mesh.Textures[i].BlendFlag;
                materialInfoPresenter.material = mesh.Textures[i].Material;
                #endif

                var meshRenderer = meshObject.AddComponent<StaticMeshRenderer>();
                var gbMaterial = mesh.Textures[i].Material;
                var blendFlag = mesh.Textures[i].BlendFlag;

                if (textures.Count == 1)
                {
                    var materialHashKey = _standardNoShadowShader.name +
                                          textures[0].name +
                                          blendFlag +
                                          gbMaterial.Emissive.r +
                                          gbMaterial.Emissive.g +
                                          gbMaterial.Emissive.b;

                    var isWaterSurface = textures[0].name
                        .StartsWith(ANIMATED_WATER_TEXTURE_DEFAULT_NAME, StringComparison.OrdinalIgnoreCase);

                    Material material;
                    if (_materials.ContainsKey(materialHashKey))
                    {
                        material = _materials[materialHashKey];
                    }
                    else
                    {
                        material = new Material(_standardNoShadowShader);
                        material.SetTexture(_mainTexturePropertyId, textures[0].texture);

                        var cutoff = blendFlag is 1 or 2 ? 0.3f : 0f;
                        if (cutoff > Mathf.Epsilon)
                        {
                            material.SetFloat(_cutoffPropertyId, cutoff);
                        }

                        if (!_disableTransparency &&
                            (gbMaterial.Emissive.r is > 0 and < 255 ||
                             gbMaterial.Emissive.g is > 0 and < 255 ||
                             gbMaterial.Emissive.b is > 0 and < 255 ||
                             isWaterSurface))
                        {
                            var transparency = blendFlag is 1 or 2 ? 0.5f : 0f;
                            if (transparency > Mathf.Epsilon)
                            {
                                material.renderQueue = TRANSPARENT_RENDER_QUEUE_INDEX;
                                material.SetFloat(_transparencyPropertyId, transparency);
                            }
                        }
                        material.SetColor(_tintColorPropertyId, _tintColor);
                        _materials[materialHashKey] = material;
                    }

                    _ = meshRenderer.Render(ref mesh.VertexInfo.Positions,
                        ref mesh.Textures[i].Triangles,
                        ref mesh.VertexInfo.Normals,
                        ref mesh.VertexInfo.Uvs[0],
                        ref material,
                        false);

                    if (isWaterSurface)
                    {
                        StartWaterSurfaceAnimation(material, textures[0].texture);
                    }
                }
                else if (textures.Count >= 2)
                {
                    var materialHashKey = _standardShader.name +
                                          textures[0].name +
                                          textures[1].name +
                                          blendFlag +
                                          gbMaterial.Emissive.r +
                                          gbMaterial.Emissive.g +
                                          gbMaterial.Emissive.b;

                    var isWaterSurface = textures[1].name
                        .StartsWith(ANIMATED_WATER_TEXTURE_DEFAULT_NAME, StringComparison.OrdinalIgnoreCase);

                    Material material;
                    if (_materials.ContainsKey(materialHashKey))
                    {
                        material = _materials[materialHashKey];
                    }
                    else
                    {
                        material = new Material(_standardShader);
                        material.SetTexture(_mainTexturePropertyId, textures[1].texture);
                        material.SetTexture(_shadowTexturePropertyId, textures[0].texture);

                        var cutoff = blendFlag is 1 or 2 ? 0.3f : 0f;
                        if (cutoff > Mathf.Epsilon)
                        {
                            material.SetFloat(_cutoffPropertyId, cutoff);
                        }

                        if (!_disableTransparency &&
                            (gbMaterial.Emissive.r is > 0 and < 255 ||
                             gbMaterial.Emissive.g is > 0 and < 255 ||
                             gbMaterial.Emissive.b is > 0 and < 255 ||
                             isWaterSurface))
                        {
                            var transparency = blendFlag is 1 or 2 ? 0.5f : 0f;
                            if (transparency > Mathf.Epsilon)
                            {
                                material.renderQueue = TRANSPARENT_RENDER_QUEUE_INDEX;
                                material.SetFloat(_transparencyPropertyId, transparency);
                            }
                        }
                        material.SetColor(_tintColorPropertyId, _tintColor);
                        _materials[materialHashKey] = material;
                    }

                    _ = meshRenderer.Render(ref mesh.VertexInfo.Positions,
                        ref mesh.Textures[i].Triangles,
                        ref mesh.VertexInfo.Normals,
                        ref mesh.VertexInfo.Uvs[1],
                        ref mesh.VertexInfo.Uvs[0],
                        ref material,
                        false);

                    if (isWaterSurface)
                    {
                        StartWaterSurfaceAnimation(material, textures[1].texture);
                    }
                }

                meshObject.transform.SetParent(transform, false);
            }
        }

        private IEnumerator AnimateWaterTexture(Material material, Texture2D defaultTexture)
        {
            var waterTextures = new List<Texture2D> { defaultTexture };

            for (var i = 2; i <= ANIMATED_WATER_ANIMATION_FRAMES; i++)
            {
                var texture = _textureProvider.GetTexture(
                    ANIMATED_WATER_TEXTURE_DEFAULT_NAME_PREFIX +
                    $"{i:00}" +
                    ANIMATED_WATER_TEXTURE_DEFAULT_EXTENSION);
                waterTextures.Add(texture);
            }

            var waterAnimationDelay = new WaitForSeconds(1 / ANIMATED_WATER_ANIMATION_FPS);

            while (isActiveAndEnabled)
            {
                for (var i = 0; i < ANIMATED_WATER_ANIMATION_FRAMES; i++)
                {
                    material.SetTexture(_mainTexturePropertyId, waterTextures[i]);
                    yield return waterAnimationDelay;
                }
            }
        }

        private void StartWaterSurfaceAnimation(Material material, Texture2D defaultTexture)
        {
            _waterAnimations.Add(StartCoroutine(AnimateWaterTexture(material, defaultTexture)));
        }

        private void OnDisable()
        {
            Dispose();
        }

        private void Dispose()
        {
            foreach (var waterAnimation in _waterAnimations)
            {
                if (waterAnimation != null)
                {
                    StopCoroutine(waterAnimation);
                }
            }

            foreach (var meshRenderer in GetComponentsInChildren<StaticMeshRenderer>())
            {
                Destroy(meshRenderer.gameObject);
            }
        }
    }
}