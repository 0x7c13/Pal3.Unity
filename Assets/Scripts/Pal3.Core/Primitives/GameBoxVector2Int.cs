// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2024, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Core.Primitives
{
    using System.Runtime.CompilerServices;

    [System.Serializable]
    public struct GameBoxVector2Int
    {
        public int X;
        public int Y;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public GameBoxVector2Int(int x, int y)
        {
            X = x;
            Y = y;
        }
    }
}