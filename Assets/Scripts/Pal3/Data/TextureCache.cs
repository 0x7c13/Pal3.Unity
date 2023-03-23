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

        public void Add(string key, Texture2D texture, bool hasAlphaChannel)
        {
            if (texture == null) return;

            key = key.ToLowerInvariant();

            if (_textureCache.ContainsKey(key))
            {
                Debug.LogError($"Texture {key} already existed in cache.");
            }
            else
            {
                texture.hideFlags = HideFlags.HideAndDontSave;
                _textureCache[key] = new (texture, hasAlphaChannel);
            }
        }

        public bool TryGetTextureFromCache(string key,
            out (Texture2D texture, bool hasAlphaChannel) texture)
        {
            key = key.ToLowerInvariant();

            if (_textureCache.ContainsKey(key))
            {
                texture = _textureCache[key];
                return true;
            }

            texture = default;
            return false;
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