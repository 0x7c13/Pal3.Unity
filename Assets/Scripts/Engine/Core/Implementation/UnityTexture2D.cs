// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Engine.Core.Implementation
{
    using Abstraction;
    using Extensions;

    public sealed class UnityTexture2D : ITexture2D
    {
        public object NativeObject => _texture2D;

        public bool IsNativeObjectDisposed => _texture2D == null;

        private UnityEngine.Texture2D _texture2D;

        public UnityTexture2D(UnityEngine.Texture2D texture2D)
        {
            _texture2D = texture2D;
        }

        public int Width => _texture2D.width;

        public int Height => _texture2D.height;

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