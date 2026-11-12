// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2025, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Engine.Core.Implementation
{
    using Abstraction;
    using Extensions;
    using UnityEngine;

    public sealed class UnityTexture2D : ITexture2D
    {
        public object NativeObject => _texture2D;

        public bool IsNativeObjectDisposed => _texture2D == null;

        private Texture2D _texture2D;
        private ITexturePixelReader _pixelReader;

        public UnityTexture2D(Texture2D texture2D, ITexturePixelReader pixelReader = null)
        {
            _texture2D = texture2D;
            _pixelReader = pixelReader;
        }

        public int Width => _texture2D.width;

        public int Height => _texture2D.height;

        public void SetWrapMode(Abstraction.WrapMode wrapMode)
        {
            _texture2D.wrapMode = wrapMode switch
            {
                Abstraction.WrapMode.Repeat => TextureWrapMode.Repeat,
                Abstraction.WrapMode.Clamp => TextureWrapMode.Clamp,
                _ => throw new System.NotImplementedException(),
            };
        }

        public (float r, float g, float b, float a) GetPixel(int x, int y)
        {
            if (_pixelReader != null)
            {
                x = _texture2D.wrapMode == TextureWrapMode.Clamp
                    ? Mathf.Clamp(x, 0, Width - 1) : (x % Width + Width) % Width;
                y = _texture2D.wrapMode == TextureWrapMode.Clamp
                    ? Mathf.Clamp(y, 0, Height - 1) : (y % Height + Height) % Height;
                var rgba = _pixelReader.GetPixel(x, y);
                return (rgba.r / 255f, rgba.g / 255f, rgba.b / 255f, rgba.a / 255f);
            }
            Color pixel = _texture2D.GetPixel(x, y);
            return (pixel.r, pixel.g, pixel.b, pixel.a);
        }

        public ISprite CreateSprite(
            float x, float y,
            float width, float height,
            float pivotX, float pivotY)
        {
            Sprite sprite = Sprite.Create(_texture2D,
                new Rect(x, y, width, height),
                new Vector2(pivotX, pivotY));

            return new UnitySprite(sprite);
        }

        public static implicit operator Texture2D(UnityTexture2D t) => t.NativeObject as Texture2D;

        public void Destroy()
        {
            if (!IsNativeObjectDisposed)
            {
                _texture2D.Destroy();
            }

            _texture2D = null;
            _pixelReader = null;
        }
    }
}
