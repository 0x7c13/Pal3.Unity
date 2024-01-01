// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2024, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Core.Primitives
{
    using System.Runtime.CompilerServices;

    [System.Serializable]
    public struct GameBoxVector2
    {
        public float X;
        public float Y;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public GameBoxVector2(float x, float y)
        {
            X = x;
            Y = y;
        }
    }
}