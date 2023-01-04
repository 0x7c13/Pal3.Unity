// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Data
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// Texture2D in-memory cache.
    /// </summary>
    public class TextureCache
    {
        private readonly Dictionary<string, (Texture2D texutre, bool hasAlphaChannel)> _textureCache = new ();

        public void Add(string textureFullPath, Texture2D texture, bool hasAlphaChannel)
        {
            if (texture == null) return;

            textureFullPath = textureFullPath.ToLower();

            if (Contains(textureFullPath))
            {
                Debug.LogError($"Texture {textureFullPath} already existed in cache.");
            }
            else
            {
                texture.hideFlags = HideFlags.HideAndDontSave;
                _textureCache[textureFullPath] = new (texture, hasAlphaChannel);
            }
        }

        public bool Contains(string textureFullPath)
        {
            textureFullPath = textureFullPath.ToLower();
            return _textureCache.ContainsKey(textureFullPath);
        }

        public (Texture2D texture, bool hasAlphaChannel) GetTextureFromCache(string textureFullPath)
        {
            textureFullPath = textureFullPath.ToLower();
            if (Contains(textureFullPath))
            {
                return _textureCache[textureFullPath];
            }
            else
            {
                throw new ArgumentException($"Texture {textureFullPath} does not exist in cache.");
            }
        }

        public void DisposeAll()
        {
            Debug.Log($"Disposing all cached textures: {_textureCache.Count}");
            foreach ((Texture2D texture, _) in _textureCache.Values)
            {
                UnityEngine.Object.Destroy(texture);
            }
            _textureCache.Clear();
        }
    }
}