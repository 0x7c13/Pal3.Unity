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
    using Core.GameBox;
    using Core.Renderer;
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
        private const float ANIMATED_WATER_TEXTURE_ALPHA = 0.6f;
        private const int ANIMATED_WATER_ANIMATION_FRAMES = 30;
        private const float ANIMATED_WATER_ANIMATION_FPS = 20f;

        private ITextureResourceProvider _textureProvider;
        private readonly List<Coroutine> _waterAnimations = new ();
        private Dictionary<string, Texture2D> _textureCache = new ();

        public void Render(PolFile polFile, ITextureResourceProvider textureProvider)
        {
            _textureProvider = textureProvider;
            _textureCache = BuildTextureCache(polFile, textureProvider);

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
                var vertices = new List<Vector3>();
                for (var j = 0; j < mesh.Vertices.Length; j++)
                {
                    var position = mesh.Vertices[j].Position;
                    vertices.Add(GameBoxInterpreter
                        .ToUnityVertex(position, GameBoxInterpreter.GameBoxUnitToUnityUnit));
                }

                var triangles = new List<int>();
                for (var j = 0; j < mesh.Textures[i].Triangles.Length; j++)
                {
                    var triangle = GameBoxInterpreter.ToUnityTriangle(mesh.Textures[i].Triangles[j]);
                    triangles.Add(triangle.x);
                    triangles.Add(triangle.y);
                    triangles.Add(triangle.z);
                }

                var normals = new List<Vector3>();
                for (var j = 0; j < mesh.Vertices.Length; j++)
                {
                    var normal = mesh.Vertices[j].Normal;
                    normals.Add(normal);
                }

                var uv1 = new List<Vector2>();
                var uv2 = new List<Vector2>();
                //var uv3 = new List<Vector2>();
                //var uv4 = new List<Vector2>();
                for (var j = 0; j < mesh.Vertices.Length; j++)
                {
                    uv1.Add(mesh.Vertices[j].Uv[0]);
                    uv2.Add(mesh.Vertices[j].Uv[1]);
                    //uv3.Add(mesh.Vertices[j].Uv[2]);
                    //uv4.Add(mesh.Vertices[j].Uv[3]);
                }

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
                    Debug.Log($"0 texture found for {meshName}");
                    return;
                }

                var meshObject = new GameObject(meshName);
                var meshRenderer = meshObject.AddComponent<StaticMeshRenderer>();

                if (textures.Count == 1)
                {
                    var isWaterSurface = textures[0].name
                        .StartsWith(ANIMATED_WATER_TEXTURE_DEFAULT_NAME, StringComparison.OrdinalIgnoreCase);

                    Material material;

                    if (isWaterSurface)
                    {
                        // TODO: Implement and use water surface shader (transparency)
                        material = new Material(Shader.Find("Pal3/StandardNoShadow"));
                        material.SetTexture(Shader.PropertyToID("_MainTex"), textures[0].texture);
                    }
                    else
                    {
                        material = new Material(Shader.Find("Pal3/StandardNoShadow"));
                        material.SetTexture(Shader.PropertyToID("_MainTex"), textures[0].texture);
                        var cutoff = (mesh.Textures[i].BlendFlag == 1) ? 0.3f : 0f;
                        if (cutoff > Mathf.Epsilon)
                        {
                            material.SetFloat(Shader.PropertyToID("_Cutoff"), cutoff);
                        }
                    }

                    meshRenderer.Render(vertices.ToArray(),
                        triangles.ToArray(),
                        normals.ToArray(),
                        uv1.ToArray(),
                        material);

                    if (isWaterSurface)
                    {
                        StartWaterSurfaceAnimation(material, textures[0].texture);
                    }
                }
                else if (textures.Count >= 2)
                {
                    var isWaterSurface = textures[1].name
                        .StartsWith(ANIMATED_WATER_TEXTURE_DEFAULT_NAME, StringComparison.OrdinalIgnoreCase);

                    Material material;

                    if (isWaterSurface)
                    {
                        // TODO: Implement and use water surface shader (transparency + shadow)
                        material = new Material(Shader.Find("Pal3/Standard"));
                        material.SetTexture(Shader.PropertyToID("_MainTex"), textures[1].texture);
                        material.SetTexture(Shader.PropertyToID("_ShadowTex"), textures[0].texture);
                    }
                    else
                    {
                        material = new Material(Shader.Find("Pal3/Standard"));
                        material.SetTexture(Shader.PropertyToID("_MainTex"), textures[1].texture);
                        material.SetTexture(Shader.PropertyToID("_ShadowTex"), textures[0].texture);
                    }

                    var cutoff = (mesh.Textures[i].BlendFlag == 1) ? 0.3f : 0f;
                    if (cutoff > Mathf.Epsilon)
                    {
                        material.SetFloat(Shader.PropertyToID("_Cutoff"), cutoff);
                    }

                    meshRenderer.Render(vertices.ToArray(),
                        triangles.ToArray(),
                        normals.ToArray(),
                        uv2.ToArray(),
                        uv1.ToArray(),
                        material);

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

            while (isActiveAndEnabled)
            {
                for (var i = 0; i < ANIMATED_WATER_ANIMATION_FRAMES; i++)
                {
                    material.SetTexture(Shader.PropertyToID("_MainTex"), waterTextures[i]);
                    yield return new WaitForSeconds(1 / ANIMATED_WATER_ANIMATION_FPS);
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