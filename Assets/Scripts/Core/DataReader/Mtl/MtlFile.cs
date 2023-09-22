// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Core.DataReader.Mtl
{
    using Primitives;

    public sealed class MtlFile
    {
        public GameBoxMaterial[] Materials { get; }

        public MtlFile(GameBoxMaterial[] materials)
        {
            Materials = materials;
        }
    }
}