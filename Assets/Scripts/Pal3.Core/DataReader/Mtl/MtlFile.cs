// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2024, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Core.DataReader.Mtl
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