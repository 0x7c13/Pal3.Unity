// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Data
{
    using Core.DataLoader;

    public interface ITextureLoaderFactory
    {
        public ITextureLoader GetTextureLoader(string fileExtension);
    }
}