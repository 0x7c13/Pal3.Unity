// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Core.Primitives
{
    using System.Runtime.CompilerServices;

    [System.Serializable]
    public struct Color32
    {
        public byte R;
        public byte G;
        public byte B;
        public byte A;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Color32(byte r, byte g, byte b, byte a)
        {
            R = r;
            G = g;
            B = b;
            A = a;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Color(Color32 c) =>
            new Color(
                (float) c.R / byte.MaxValue,
                (float) c.G / byte.MaxValue,
                (float) c.B / byte.MaxValue,
                (float) c.A / byte.MaxValue);
    }
}