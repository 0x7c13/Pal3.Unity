// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2024, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Engine.Services
{
    using System.Collections.Generic;
    using Core.Abstraction;
    using Logging;

    /// <summary>
    /// Texture2D in-memory cache.
    /// </summary>
    public sealed class TextureCache : ITextureCache
    {
        private readonly Dictionary<string, (ITexture2D texutre, bool hasAlphaChannel)> _textureCache = new ();

        public void Add(string key, ITexture2D texture, bool hasAlphaChannel)
        {
            if (texture == null) return;

            key = key.ToLowerInvariant();

            if (_textureCache.ContainsKey(key))
            {
                EngineLogger.LogError($"Texture {key} already existed in cache");
            }
            else
            {
                _textureCache[key] = new (texture, hasAlphaChannel);
            }
        }

        public bool TryGet(string key, out (ITexture2D texture, bool hasAlphaChannel) texture)
        {
            key = key.ToLowerInvariant();

            if (_textureCache.TryGetValue(key, out (ITexture2D texutre, bool hasAlphaChannel) textureInCache))
            {
                texture = textureInCache;
                return true;
            }

            texture = default;
            return false;
        }

        public void DisposeAll()
        {
            EngineLogger.Log($"Disposing {_textureCache.Count} cached textures");
            foreach ((ITexture2D texture, _) in _textureCache.Values)
            {
                texture.Destroy();
            }
            _textureCache.Clear();
        }
    }
}