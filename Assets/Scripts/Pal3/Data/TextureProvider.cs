// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Data
{
    using System.IO;
    using Core.DataLoader;
    using Core.DataReader.Cpk;
    using Core.FileSystem;
    using UnityEngine;

    public class TextureProvider : TextureProviderBase, ITextureResourceProvider
    {
        private readonly ICpkFileSystem _fileSystem;
        private readonly ITextureLoaderFactory _textureLoaderFactory;
        private readonly TextureCache _textureCache;
        private readonly string _relativePath;

        public TextureProvider(ICpkFileSystem fileSystem,
            ITextureLoaderFactory textureLoaderFactory,
            string relativePath,
            TextureCache textureCache = null)
        {
            if (!relativePath.EndsWith(CpkConstants.CpkDirectorySeparatorChar))
            {
                relativePath += CpkConstants.CpkDirectorySeparatorChar;
            }

            _fileSystem = fileSystem;
            _textureLoaderFactory = textureLoaderFactory;
            _relativePath = relativePath;
            _textureCache = textureCache;
        }

        private Texture2D GetTextureInCache(string textureFullPath, out bool hasAlphaChannel)
        {
            if (_textureCache != null && _textureCache.Contains(textureFullPath))
            {
                var textureInfo = _textureCache.GetTextureFromCache(textureFullPath);
                hasAlphaChannel = textureInfo.hasAlphaChannel;
                return textureInfo.texture;
            }

            hasAlphaChannel = false;
            return null;
        }

        public Texture2D GetTexture(string name)
        {
            return GetTexture(name, out _);
        }

        public Texture2D GetTexture(string name, out bool hasAlphaChannel)
        {
            if (string.IsNullOrEmpty(name))
            {
                hasAlphaChannel = false;
                return Texture2D.whiteTexture;
            }

            var texturePath = _relativePath + name;

            // Note: Most texture files used in (.pol, .cvd) files are stored inside Pal3's CPack
            // archives and they are pre-compressed to DXT format (DXT1 & DXT3). So we need to
            // swap the file extension from original texture format (.bmp, .tga etc.) to dds format.
            var ddsTexturePath = Path.ChangeExtension(texturePath, ".dds");

            if (_textureCache != null)
            {
                // Check if dds texture exists in cache
                if (GetTextureInCache(ddsTexturePath, out hasAlphaChannel) is { } ddsTextureInCache)
                {
                    return ddsTextureInCache;
                }

                // Check if texture exists in cache
                if (GetTextureInCache(texturePath, out hasAlphaChannel) is { } textureInCache)
                {
                    return textureInCache;
                }
            }

            // Get dds texture
            var textureLoader = _textureLoaderFactory.GetTextureLoader(".dds");
            var texture = base.GetTexture(_fileSystem, ddsTexturePath, textureLoader, out hasAlphaChannel);
            if (texture != null)
            {
                _textureCache?.Add(ddsTexturePath, texture, hasAlphaChannel);
            }
            else // If dds texture does not exist, try original extension
            {
                textureLoader = _textureLoaderFactory.GetTextureLoader(Path.GetExtension(name));
                texture = base.GetTexture(_fileSystem, texturePath, textureLoader, out hasAlphaChannel);
                if (texture == null) Debug.LogWarning($"Texture not found: {texturePath}");
                else _textureCache?.Add(texturePath, texture, hasAlphaChannel);
            }

            return texture;
        }
    }
}