// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2025, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Core.Primitives
{
    public enum GameBoxBlendFlag
    {
        Opaque,             // opaque
        AlphaBlend,         // src.alpha, 1-src.alpha, +
        InvertColorBlend,   // srcAlpha, One , +
        AdditiveBlend,      //
    }
}