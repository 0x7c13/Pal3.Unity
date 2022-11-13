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
        private const string ANIMATED_WATER_TEXTURE_DEFAULT_NAME_PREFIX = "w00";
        private const string ANIMATED_WATER_TEXTURE_DEFAULT_NAME = "w0001";
        private const string ANIMATED_WATER_TEXTURE_DEFAULT_EXTENSION = ".dds";
        private const int ANIMATED_WATER_ANIMATION_FRAMES = 30;
        private const float ANIMATED_WATER_ANIMATION_FPS = 20f;

        private ITextureResourceProvider _textureProvider;
        private IMaterialFactory _materialFactory;
        private readonly List<Coroutine> _waterAnimations = new ();
        private Dictionary<string, Texture2D> _textureCache = new ();

        private Color _tintColor;

        private readonly int _mainTexturePropertyId = Shader.PropertyToID("_MainTex");
        
        public void Render(PolFile polFile,
            IMaterialFactory materialFactory,
            ITextureResourceProvider textureProvider,
            Color tintColor)
        {
            _materialFactory = materialFactory;
            _textureProvider = textureProvider;
            _tintColor = tintColor;
            _textureCache = BuildTextureCache(polFile, textureProvider);
            
            for (var i = 0; i < polFile.Meshes.Length; i++)
            {
                RenderMeshInternal(
                    polFile.NodeDescriptions[i],
                    polFile.Meshes[i]);
            }
        }

        public Bounds GetRendererBounds()
        {
            var bounds = new Bounds (transform.position, Vector3.one);

            foreach (StaticMeshRenderer meshRenderer in GetComponentsInChildren<StaticMeshRenderer>())
            {
                bounds.Encapsulate(meshRenderer.GetRendererBounds());
            }

            return bounds;
        }

        private Dictionary<string, Texture2D> BuildTextureCache(PolFile polFile,
            ITextureResourceProvider textureProvider)
        {
            Dictionary<string, Texture2D> textureCache = new();
            foreach (PolMesh mesh in polFile.Meshes)
            {
                foreach (PolTexture texture in mesh.Textures)
                {
                    foreach (var textureName in texture.TextureNames)
                    {
                        if (string.IsNullOrEmpty(textureName)) continue;
                        if (textureCache.ContainsKey(textureName)) continue;
                        #if RTX_ON
                        // No need to load pre-baked shadow texture for lighting enabled scene
                        // Note: all shadow texture name starts with "^"
                        Texture2D texture2D = textureName.StartsWith("^") ? null : textureProvider.GetTexture(textureName);
                        #else
                        Texture2D texture2D = textureProvider.GetTexture(textureName);
                        #endif
                        textureCache[textureName] = texture2D;
                    }
                }
            }
            return textureCache;
        }

        private void RenderMeshInternal(PolGeometryNode meshNode, PolMesh mesh)
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
                    Debug.LogWarning($"0 texture found for {meshNode.Name}");
                    return;
                }

                var meshObject = new GameObject(meshNode.Name);

                // Attach BlendFlag and GameBoxMaterial to the GameObject for better debuggability
                #if UNITY_EDITOR
                var materialInfoPresenter = meshObject.AddComponent<MaterialInfoPresenter>();
                materialInfoPresenter.blendFlag = mesh.Textures[i].BlendFlag;
                materialInfoPresenter.material = mesh.Textures[i].Material;
                #endif

                var meshRenderer = meshObject.AddComponent<StaticMeshRenderer>();
                var blendFlag = mesh.Textures[i].BlendFlag;

                if (textures.Count == 1)
                {
                    Material[] materials;
                    
                    var isWaterSurface = textures[0].name
                        .StartsWith(ANIMATED_WATER_TEXTURE_DEFAULT_NAME, StringComparison.OrdinalIgnoreCase);

                    if (isWaterSurface)
                    {
                        materials = new Material[1];
                        // get alpha from first pixel of the water texture
                        float waterSurfaceOpacity = textures[0].texture.GetPixel(0, 0).a;
                        materials[0] = _materialFactory.CreateWaterMaterial(
                            textures[0].texture,
                            shadowTexture: null,
                            waterSurfaceOpacity);
                        StartWaterSurfaceAnimation(materials[0], textures[0].texture);
                    }
                    else
                    {
                        materials = _materialFactory.CreateStandardMaterials(
                            RendererType.Pol,
                            textures[0].texture,
                            shadowTexture: null,
                            _tintColor,
                            blendFlag);
                    }

                    _ = meshRenderer.Render(ref mesh.VertexInfo.Positions,
                        ref mesh.Textures[i].Triangles,
                        ref mesh.VertexInfo.Normals,
                        ref mesh.VertexInfo.Uvs[0],
                        ref mesh.VertexInfo.Uvs[1],
                        ref materials,
                        false);
                }
                else if (textures.Count >= 2)
                {
                    Material[] materials;
                    
                    var isWaterSurface = textures[1].name
                        .StartsWith(ANIMATED_WATER_TEXTURE_DEFAULT_NAME, StringComparison.OrdinalIgnoreCase);
                    
                    if (isWaterSurface)
                    {
                        materials = new Material[1];
                        // get alpha from first pixel of the water texture
                        float waterSurfaceOpacity = textures[1].texture.GetPixel(0, 0).a;
                        materials[0] = _materialFactory.CreateWaterMaterial(
                            textures[1].texture,
                            textures[0].texture,
                            waterSurfaceOpacity);
                        StartWaterSurfaceAnimation(materials[0], textures[1].texture);
                    }
                    else
                    {
                        materials = _materialFactory.CreateStandardMaterials(
                            RendererType.Pol,
                            textures[1].texture,
                            textures[0].texture,
                            _tintColor,
                            blendFlag);
                    }

                    _ = meshRenderer.Render(ref mesh.VertexInfo.Positions,
                        ref mesh.Textures[i].Triangles,
                        ref mesh.VertexInfo.Normals,
                        ref mesh.VertexInfo.Uvs[1],
                        ref mesh.VertexInfo.Uvs[0],
                        ref materials,
                        false);
                }

                meshObject.transform.SetParent(transform, false);
            }
        }

        private IEnumerator AnimateWaterTexture(Material material, Texture2D defaultTexture)
        {
            var waterTextures = new List<Texture2D> { defaultTexture };

            for (var i = 2; i <= ANIMATED_WATER_ANIMATION_FRAMES; i++)
            {
                Texture2D texture = _textureProvider.GetTexture(
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
            foreach (Coroutine waterAnimation in _waterAnimations)
            {
                if (waterAnimation != null)
                {
                    StopCoroutine(waterAnimation);
                }
            }

            foreach (StaticMeshRenderer meshRenderer in GetComponentsInChildren<StaticMeshRenderer>())
            {
                Destroy(meshRenderer.gameObject);
            }
        }
    }
}