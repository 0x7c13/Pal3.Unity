// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2025, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Engine.DataLoader
{
    using Core.Abstraction;
    using Pal3.Core.DataReader.Dxt;

    public sealed class DxtTextureLoader : ITextureLoader
    {
        public ITextureDataSource Load(byte[] data, out bool hasAlphaChannel)
        {
            var image = new DxtImage(data);
            hasAlphaChannel = image.HasAlphaChannel;
            return new DxtTextureDataSource(image);
        }
    }
}
