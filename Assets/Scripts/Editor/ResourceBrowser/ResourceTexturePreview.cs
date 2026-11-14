// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.ResourceBrowser
{
    using System;
    using System.IO;
    using Engine.Core.Abstraction;
    using Engine.Core.Implementation;
    using Game.Data;
    using UnityEditor;
    using UnityEngine;

    public sealed class ResourceTexturePreview : IDisposable
    {
        public Texture2D Texture
        {
            get; private set;
        }

        private UnityEditor.Editor _editor;

        public ResourceTexturePreview(ResourceEntry entry, byte[] bytes)
        {
            try
            {
                bytes = ResourceImageData.Prepare(entry.Extension, bytes);
                if (entry.Extension is ".png" or ".jpg" or ".jpeg")
                {
                    Texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                    if (!Texture.LoadImage(bytes))
                        throw new InvalidDataException("Unity could not decode this image.");
                }
                else
                {
                    using ITextureDataSource source = new TextureLoaderFactory().GetTextureLoader(entry.Extension).Load(bytes, out _);
                    if ((long)source.Width * source.Height > 32 * 1024 * 1024)
                        throw new InvalidDataException("Image exceeds the 32 megapixel preview limit.");
                    Texture = (Texture2D)new UnityTextureFactory().CreateTexture(source, TextureCreateOptions.Readable).NativeObject;
                }
                Texture.name = entry.Name;
                Texture.hideFlags = HideFlags.HideAndDontSave;
                _editor = UnityEditor.Editor.CreateEditor(Texture);
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        public void DrawSettings() => _editor?.OnPreviewSettings();

        public void Draw(Rect rect)
        {
            if (_editor != null)
                _editor.DrawPreview(rect);
        }

        public void SavePng(string path)
        {
            byte[] bytes = Texture.EncodeToPNG();
            ResourceCatalog.WriteNewFile(path, stream => stream.Write(bytes, 0, bytes.Length));
        }

        public void Dispose()
        {
            if (_editor != null)
                UnityEngine.Object.DestroyImmediate(_editor);
            if (Texture != null)
                UnityEngine.Object.DestroyImmediate(Texture);
            _editor = null;
            Texture = null;
        }
    }
}
