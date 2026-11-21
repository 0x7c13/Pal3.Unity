// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Battle
{
    using System;

    // CTrail's ring advances once per Update, independently of render calls or
    // enablement. The published trail.gbf uses diffuse alpha and additive blend.
    public sealed class BattleTrailState
    {
        public const int Capacity = 8;
        private float _remainder;

        public uint Frame
        {
            get; private set;
        }

        public int CurrentTexture => (int)(Frame % Capacity);

        public void Advance(double scaledSeconds)
        {
            if (!double.IsFinite(scaledSeconds) || scaledSeconds < 0)
                throw new ArgumentOutOfRangeException(nameof(scaledSeconds));
            _remainder += (float)scaledSeconds;
            if (_remainder >= .02f)
            {
                Frame = unchecked(Frame + 1);
                _remainder -= .02f;
            }
        }

        public int TextureAt(int drawOrder)
        {
            if (drawOrder < 0 || drawOrder >= Capacity)
                throw new ArgumentOutOfRangeException(nameof(drawOrder));
            return (CurrentTexture + 1 + drawOrder) % Capacity;
        }

        public static byte AlphaAt(int drawOrder)
        {
            if (drawOrder < 0 || drawOrder >= Capacity)
                throw new ArgumentOutOfRangeException(nameof(drawOrder));
            return (byte)(158 - (Capacity - 1 - drawOrder) * 8);
        }

        public static float ContributionAt(int drawOrder)
        {
            int alpha = AlphaAt(drawOrder);
            return alpha / 255f * (alpha / 2) / 255f;
        }
    }
}
