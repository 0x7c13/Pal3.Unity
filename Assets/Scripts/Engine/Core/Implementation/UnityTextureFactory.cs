// ------------------------------------------------------------------------
//  Copyright (c) 2021-2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Engine.Core.Implementation
{
    using System;
    using System.Buffers;
    using Abstraction;
    using UnityEngine;
    using UnityEngine.Experimental.Rendering;

    public sealed class UnityTextureFactory : ITextureFactory
    {
        public ITexture2D CreateTexture(int width, int height, byte[] rgba32DataBuffer)
        {
            using var source = new Rgba32TextureDataSource(width, height, rgba32DataBuffer);
            return CreateTexture(source, TextureCreateOptions.Readable);
        }

        public ITexture2D CreateTexture(ITextureDataSource source, TextureCreateOptions options)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            int mipCount = options.Mipmaps switch
            {
                TextureMipMode.BaseOnly => 1,
                TextureMipMode.Preserve or TextureMipMode.GenerateIfMissing => source.MipCount,
                _ => throw new ArgumentOutOfRangeException(nameof(options)),
            };
            bool generateMips = options.Mipmaps == TextureMipMode.GenerateIfMissing && mipCount == 1;
            GraphicsFormat format = source.Format switch
            {
                TextureDataFormat.Rgba32 => GraphicsFormat.R8G8B8A8_SRGB,
                TextureDataFormat.Bc1 => GraphicsFormat.RGBA_DXT1_SRGB,
                TextureDataFormat.Bc2 => GraphicsFormat.RGBA_DXT3_SRGB,
                _ => throw new NotSupportedException($"Unsupported texture data format: {source.Format}"),
            };
            Texture2D texture = null;
            try
            {
                // Native blocks cannot synthesize missing mips or serve readable sprite pixels.
                bool uploadCompressed = source.Format != TextureDataFormat.Rgba32 &&
                    options.PreferNativeCompression && !options.KeepCpuReadable && !generateMips &&
                    source.Width % 4 == 0 && source.Height % 4 == 0 &&
                    SystemInfo.IsFormatSupported(format, GraphicsFormatUsage.Sample);
                if (uploadCompressed)
                {
                    texture = new Texture2D(source.Width, source.Height, format, mipCount, TextureCreationFlags.None);
                    for (int mip = 0; mip < mipCount; mip++)
                    {
                        var data = source.GetMipData(mip);
                        texture.SetPixelData(data.Array, mip, data.Offset);
                    }
                    texture.Apply(updateMipmaps: false, makeNoLongerReadable: true);
                }
                else
                {
                    texture = new Texture2D(source.Width, source.Height, TextureFormat.RGBA32,
                        generateMips ? -1 : mipCount, linear: false);
                    byte[] rgba = source.Format == TextureDataFormat.Rgba32 ? null :
                        ArrayPool<byte>.Shared.Rent(checked(source.Width * source.Height * 4));
                    try
                    {
                        for (int mip = 0; mip < mipCount; mip++)
                        {
                            if (rgba == null)
                            {
                                var data = source.GetMipData(mip);
                                texture.SetPixelData(data.Array, mip, data.Offset);
                            }
                            else
                            {
                                source.CopyMipToRgba32(mip, rgba);
                                texture.SetPixelData(rgba, mip);
                            }
                        }
                        texture.Apply(updateMipmaps: generateMips, makeNoLongerReadable: !options.KeepCpuReadable);
                    }
                    finally { if (rgba != null) ArrayPool<byte>.Shared.Return(rgba); }
                }
                if (texture.mipmapCount > 1) texture.filterMode = FilterMode.Trilinear;
                return new UnityTexture2D(texture, options.KeepCpuReadable ? null : source.CreatePixelReader());
            }
            catch
            {
                if (texture != null) UnityEngine.Object.Destroy(texture);
                throw;
            }
        }

        public ITexture2D CreateWhiteTexture() => new UnityTexture2D(Texture2D.whiteTexture);
    }
}
