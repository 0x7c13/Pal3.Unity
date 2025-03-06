// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2025, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Core.Primitives
{
    public static class GameBoxVertexType
    {
        public const int Null        = 0;
        public const int MaxUvSet    = 4;
        public const int XYZ         = (1 << 0);
        public const int Normal      = (1 << 1);
        public const int Diffuse     = (1 << 2);
        public const int Specular    = (1 << 3);
        public const int UV0         = (1 << 4);
        public const int UV1         = (1 << 5);
        public const int UV2         = (1 << 6);
        public const int UV3         = (1 << 7);
        public const int XYZRHW      = (1 << 8);
        public const int FlagMask    = (1 << 31);
    }
}