// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE.txt in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Data
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;

    public class TextureCache
    {
        private readonly Dictionary<string, (Texture2D texutre, bool hasAlphaChannel)> _textureCache = new ();

        public void AddTextureToCache(string textureFullPath, Texture2D texture, bool hasAlphaChannel)
        {
            if (texture == null) return;

            textureFullPath = textureFullPath.ToLower();

            if (IsTextureAvailableInCache(textureFullPath))
            {
                Debug.LogError($"Texture {textureFullPath} already existed in cache.");
            }
            else
            {
                texture.hideFlags = HideFlags.HideAndDontSave;
                _textureCache[textureFullPath] = new (texture, hasAlphaChannel);
            }
        }

        public bool IsTextureAvailableInCache(string textureFullPath)
        {
            textureFullPath = textureFullPath.ToLower();
            return _textureCache.ContainsKey(textureFullPath);
        }

        public (Texture2D texture, bool hasAlphaChannel) GetTextureFromCache(string textureFullPath)
        {
            textureFullPath = textureFullPath.ToLower();
            if (IsTextureAvailableInCache(textureFullPath))
            {
                return _textureCache[textureFullPath];
            }
            else
            {
                throw new ArgumentException($"Texture {textureFullPath} does not exist in cache.");
            }
        }

        public void DisposeAllCachedTextures()
        {
            Debug.Log($"Disposing all cached textures: {_textureCache.Count}");
            foreach (var textureTuple in _textureCache.Values)
            {
                UnityEngine.Object.Destroy(textureTuple.texutre);
            }
            _textureCache.Clear();
        }
    }
}