// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
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

        public UnityTexture2D(Texture2D texture2D)
        {
            _texture2D = texture2D;
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

        public void Destroy()
        {
            if (!IsNativeObjectDisposed)
            {
                _texture2D.Destroy();
            }

            _texture2D = null;
        }
    }
}