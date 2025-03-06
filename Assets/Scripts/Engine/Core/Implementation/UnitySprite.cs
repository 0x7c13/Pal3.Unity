// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2025, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Engine.Core.Implementation
{
    using Abstraction;
    using Extensions;
    using UnityEngine;

    public sealed class UnitySprite : ISprite
    {
        public object NativeObject => _sprite;

        public bool IsNativeObjectDisposed  => _sprite == null;

        private Sprite _sprite;

        public ITexture2D Texture { get; }

        public UnitySprite(Sprite sprite)
        {
            _sprite = sprite;
            Texture = new UnityTexture2D(sprite.texture);
        }

        public static implicit operator Sprite(UnitySprite s) => s.NativeObject as Sprite;

        public void Destroy()
        {
            if (!IsNativeObjectDisposed)
            {
                _sprite.Destroy();
            }

            _sprite = null;
        }
    }
}