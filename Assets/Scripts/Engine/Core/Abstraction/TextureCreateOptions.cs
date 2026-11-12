// See LICENSE in the project root.

namespace Engine.Core.Abstraction
{
    public enum TextureMipMode
    {
        BaseOnly,
        Preserve,
        GenerateIfMissing,
    }

    /// <summary>Resolved creation requirements; contains no game-specific sampling policy.</summary>
    public readonly struct TextureCreateOptions
    {
        public TextureMipMode Mipmaps { get; }
        public bool PreferNativeCompression { get; }
        public bool KeepCpuReadable { get; }

        public TextureCreateOptions(TextureMipMode mipmaps, bool preferNativeCompression, bool keepCpuReadable)
        {
            Mipmaps = mipmaps;
            PreferNativeCompression = preferNativeCompression;
            KeepCpuReadable = keepCpuReadable;
        }

        public static TextureCreateOptions Readable => new(TextureMipMode.BaseOnly,
            preferNativeCompression: false, keepCpuReadable: true);
    }
}
