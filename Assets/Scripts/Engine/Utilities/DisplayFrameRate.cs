// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Engine.Utilities
{
    using System;
    using UnityEngine;

    public static class DisplayFrameRate
    {
        public static double RefreshRate => ValidRefreshRate(Screen.currentResolution.refreshRateRatio.value);

        public static double ValidRefreshRate(double refreshRate) =>
            double.IsFinite(refreshRate) && refreshRate >= 1 && refreshRate <= int.MaxValue ? refreshRate : 60;

        // Desktop and Web ignore the software target while v-sync is active.
        public static int VSyncForLimit(int configured, int requested, double refreshRate) =>
            configured > 0 && requested > 0 && ValidRefreshRate(refreshRate) / configured > requested ? 0 : configured;

        // Unity's target is integral; flooring keeps it at or below fractional
        // display rates too. Lower user limits remain valid.
        public static int Limit(int requested, double refreshRate)
        {
            int maximum = (int)ValidRefreshRate(refreshRate);
            return requested > 0 ? Math.Min(requested, maximum) : maximum;
        }
    }

    // A presentation clock only: simulation and audio must advance separately.
    public sealed class DisplayFramePacer
    {
        private double _lastFrame = double.NegativeInfinity;

        public bool TryAdvance(double now, double refreshRate)
        {
            if (!double.IsFinite(now))
                return false;
            // Floating-point rounding must not defer a callback that arrives
            // exactly one display period after the previous frame.
            const double timestampTolerance = 1e-9;
            if (now >= _lastFrame && now - _lastFrame + timestampTolerance < 1 / DisplayFrameRate.ValidRefreshRate(refreshRate))
                return false;
            _lastFrame = now;
            return true;
        }
    }
}
