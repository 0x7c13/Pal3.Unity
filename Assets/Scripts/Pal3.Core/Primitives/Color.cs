// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2024, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Core.Primitives
{
    using System.Runtime.CompilerServices;

    [System.Serializable]
    public struct Color
    {
        public float R;
        public float G;
        public float B;
        public float A;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Color(float r, float g, float b, float a)
        {
            R = r;
            G = g;
            B = b;
            A = a;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Color(float r, float g, float b)
        {
            R = r;
            G = g;
            B = b;
            A = 1f;
        }

        public static Color White
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new (1f, 1f, 1f, 1f);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Color32(Color c) =>
            new Color32(
                (byte) (c.R * byte.MaxValue),
                (byte) (c.G * byte.MaxValue),
                (byte) (c.B * byte.MaxValue),
                (byte) (c.A * byte.MaxValue));
    }
}