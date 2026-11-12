// See LICENSE in the project root.

namespace Engine.Core.Abstraction
{
    /// <summary>Owned, immutable CPU pixel data, independent of upload-buffer lifetime.</summary>
    public interface ITexturePixelReader
    {
        /// <summary>Read an in-bounds base-level pixel in RGBA byte order.</summary>
        (byte r, byte g, byte b, byte a) GetPixel(int x, int y);
    }
}
