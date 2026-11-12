// See LICENSE in the project root.

namespace Pal3.Game.Data
{
    using System;
    using Engine.Core.Abstraction;

    public enum TextureUsage
    {
        World,
        Sprite,
        Skybox,
    }

    internal static class TextureCreationPolicy
    {
        private const int MinimumGeneratedMipSize = 64;

        public static TextureCreateOptions Resolve(ITextureDataSource source, TextureUsage usage)
        {
            if (usage == TextureUsage.Sprite) return TextureCreateOptions.Readable;
            // Six independently authored sky faces retain their original base-level sampling.
            if (usage == TextureUsage.Skybox)
                return new TextureCreateOptions(TextureMipMode.BaseOnly,
                    preferNativeCompression: true, keepCpuReadable: false);
            // BMP/TGA world textures retain their original readable, base-only sampling for now.
            if (source.Format == TextureDataFormat.Rgba32) return TextureCreateOptions.Readable;

            // Alpha textures use complementary cutout/blend passes. Preserve base coverage
            // until coverage-preserving mipmaps are validated against both cutoffs.
            TextureMipMode mipmaps = TextureMipMode.BaseOnly;
            if (!source.HasTransparency)
            {
                if (source.MipCount > 1) mipmaps = TextureMipMode.Preserve;
                // Small baked-light tiles gain little from generated chains; authored mips stay.
                else if (Math.Max(source.Width, source.Height) >= MinimumGeneratedMipSize)
                    mipmaps = TextureMipMode.GenerateIfMissing;
            }

            return new TextureCreateOptions(mipmaps, preferNativeCompression: true, keepCpuReadable: false);
        }
    }
}
