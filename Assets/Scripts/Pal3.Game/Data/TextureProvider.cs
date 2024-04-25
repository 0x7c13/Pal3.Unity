// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2024, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Game.Data
{
    using System;
    using System.IO;
    using Core.DataReader.Cpk;
    using Core.FileSystem;
    using Engine.Core.Abstraction;
    using Engine.Logging;
    using Engine.Services;

    public sealed class TextureProvider : TextureProviderBase, ITextureResourceProvider
    {
        private readonly IFileSystem _fileSystem;
        private readonly ITextureLoaderFactory _textureLoaderFactory;
        private readonly ITextureFactory _textureFactory;
        private readonly ITextureCache _textureCache;
        private readonly string _relativeDirectoryPath;

        public TextureProvider(IFileSystem fileSystem,
            ITextureLoaderFactory textureLoaderFactory,
            ITextureFactory textureFactory,
            string relativeDirectoryPath,
            ITextureCache textureCache = null)
        {
            if (!relativeDirectoryPath.EndsWith(CpkConstants.DirectorySeparatorChar))
            {
                relativeDirectoryPath += CpkConstants.DirectorySeparatorChar;
            }

            _fileSystem = fileSystem;
            _textureLoaderFactory = textureLoaderFactory;
            _textureFactory = textureFactory;
            _relativeDirectoryPath = relativeDirectoryPath.ToLower(); // Texture path is case insensitive
            _textureCache = textureCache;
        }

        private bool TryGetTextureFromCache(string textureFullPath,
            out (ITexture2D texture, bool hasAlphaChannel) texture)
        {
            if (_textureCache != null)
            {
                return _textureCache.TryGet(textureFullPath, out texture);
            }

            texture = default;
            return false;
        }

        public string GetTexturePath(string name)
        {
            return _relativeDirectoryPath + name;
        }

        public ITexture2D GetTexture(string name)
        {
            return GetTexture(name, out _);
        }

        public ITexture2D GetTexture(string name, out bool hasAlphaChannel)
        {
            // Return if name is invalid
            if (string.IsNullOrEmpty(name) || !name.Contains('.'))
            {
                hasAlphaChannel = false;
                return _textureFactory.CreateWhiteTexture();
            }

            string texturePath = _relativeDirectoryPath + name;

            // Note: Most texture files used in (.pol, .cvd) files are stored inside Pal3's CPack
            // archives and they are pre-compressed to DXT format (DXT1 & DXT3). So we need to
            // swap the file extension from original texture format (.bmp, .tga etc.) to dds format.
            string ddsTexturePath = null;
            try
            {
                ddsTexturePath = Path.ChangeExtension(texturePath, ".dds");
            }
            catch (Exception ex)
            {
                EngineLogger.LogWarning($"Failed to change path extension for texture: {texturePath}, ex: {ex}");
                hasAlphaChannel = false;
                return _textureFactory.CreateWhiteTexture();
            }

            if (_textureCache != null)
            {
                // Check if dds texture exists in cache
                if (TryGetTextureFromCache(ddsTexturePath,
                        out (ITexture2D texture, bool hasAlphaChannel) ddsTextureInCache))
                {
                    hasAlphaChannel = ddsTextureInCache.hasAlphaChannel;
                    return ddsTextureInCache.texture;
                }

                // Check if texture exists in cache
                if (TryGetTextureFromCache(texturePath,
                        out (ITexture2D texture, bool hasAlphaChannel) textureInCache))
                {
                    hasAlphaChannel = textureInCache.hasAlphaChannel;
                    return textureInCache.texture;
                }
            }

            // Get dds texture
            ITextureLoader textureLoader = _textureLoaderFactory.GetTextureLoader(".dds");
            ITexture2D texture = base.GetTexture(_fileSystem, ddsTexturePath, textureLoader, out hasAlphaChannel);
            if (texture != null)
            {
                _textureCache?.Add(ddsTexturePath, texture, hasAlphaChannel);
            }
            else // If dds texture does not exist, try original extension
            {
                textureLoader = _textureLoaderFactory.GetTextureLoader(Path.GetExtension(name));
                texture = base.GetTexture(_fileSystem, texturePath, textureLoader, out hasAlphaChannel);
                if (texture == null) EngineLogger.LogWarning($"Texture not found: {texturePath}");
                else _textureCache?.Add(texturePath, texture, hasAlphaChannel);
            }

            return texture;
        }
    }
}