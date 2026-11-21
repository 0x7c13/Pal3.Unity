// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Game.Rendering.Renderer
{
    using System;

    /// <summary>An explicit material-group appearance override, independent of gameplay and resource naming.</summary>
    public readonly struct PolSurfaceAppearance : IEquatable<PolSurfaceAppearance>
    {
        public bool UnlitBackground
        {
            get;
        }

        public bool GeometricNormals
        {
            get;
        }

        public static PolSurfaceAppearance Painted(float brightness = 1) => new(true, false, brightness);

        public static PolSurfaceAppearance RecalculateNormals => new(false, true, 1);

        public float Brightness
        {
            get;
        }

        private PolSurfaceAppearance(bool unlitBackground, bool geometricNormals, float brightness)
        {
            UnlitBackground = unlitBackground;
            GeometricNormals = geometricNormals;
            Brightness = brightness;
            Validate();
        }

        public void Validate()
        {
            if (!float.IsFinite(Brightness) || Brightness < 0 || Brightness > 2)
                throw new ArgumentOutOfRangeException(nameof(Brightness), "Background brightness must be finite and between zero and two.");
        }

        public bool Equals(PolSurfaceAppearance other) => UnlitBackground == other.UnlitBackground && GeometricNormals == other.GeometricNormals && Brightness.Equals(other.Brightness);

        public override bool Equals(object other) => other is PolSurfaceAppearance appearance && Equals(appearance);

        public override int GetHashCode() => HashCode.Combine(UnlitBackground, GeometricNormals, Brightness);
    }
}
